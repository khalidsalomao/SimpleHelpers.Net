#region *   License     *
/*
    SimpleHelpers - SQLiteStorage   

    Copyright © 2013 Khalid Salomão

    Permission is hereby granted, free of charge, to any person
    obtaining a copy of this software and associated documentation
    files (the “Software”), to deal in the Software without
    restriction, including without limitation the rights to use,
    copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS IN THE SOFTWARE. 

    License: http://www.opensource.org/licenses/mit-license.php
    Website: https://github.com/khalidsalomao/SimpleHelpers.Net
 */
#endregion

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using Dapper;

namespace $rootnamespace$.SimpleHelpers.SQLite
{
    public class SQLiteStorage<T> where T : class
    {
        const int cacheSize = 1000;

        public string TableName { get; set; }

        public SQLiteStorage (string filename) :
            this (filename, typeof (T).Name)
        {
        }

        public SQLiteStorage (string filename, string tableName)
        {
            if (String.IsNullOrEmpty (tableName))
                throw new ArgumentNullException ("TableName");
            TableName = tableName;
            Configure (filename, cacheSize);
        }

        protected string m_connectionString = null;

        protected void Configure (string filename, int cacheSize = 1500)
        {
            // sanity check
            if (String.IsNullOrEmpty (filename))
                throw new ArgumentNullException ("filename");
            // create connection string
            var sb = new SQLiteConnectionStringBuilder ();
            sb.DataSource = filename;
            sb.FailIfMissing = false;
            sb.PageSize = 32768;
            sb.CacheSize = cacheSize;
            sb.ForeignKeys = false;
            sb.UseUTF16Encoding = false;
            sb.Pooling = true;
            sb.JournalMode = SQLiteJournalModeEnum.Wal;
            sb.SyncMode = SynchronizationModes.Normal;
            m_connectionString = sb.ToString ();
            // execute initialization
            CreateTable ();
        }

        protected SQLiteConnection Open ()
        {
            if (m_connectionString == null)
            {
                throw new ArgumentNullException ("Invalid connection string, call Configure to set the connection string.");
            }
            var connection = new SQLiteConnection (m_connectionString);
            connection.Open ();
            return connection;
        }

        protected void CreateTable ()
        {
            CreateTable (Open (),TableName, GetTableCreateSQL ());
        }

        protected static void CreateTable (SQLiteConnection connection, string tableName, params string[] tableCreationSql)
        {
            if (connection.Query<Int64> ("SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@table", new { table = tableName }).FirstOrDefault () == 0)
            {
                foreach (var sql in tableCreationSql)
                {
                    connection.Execute (sql);
                }
            }
        }

        public void Vaccum ()
        {
            SQLiteConnection.ClearAllPools ();
            using (var db = Open ())
            {
                db.Execute ("vacuum");
            }
        }
        
        protected string[] GetTableCreateSQL ()
        {
            return new string[]
            {
                "CREATE TABLE IF NOT EXISTS \"" + TableName + "\" (Id integer NOT NULL PRIMARY KEY AUTOINCREMENT, " +
                "Date datetime NOT NULL DEFAULT 'CURRENT_TIMESTAMP', " +
                "Key varchar NOT NULL," +
                "Value varchar NOT NULL)",
                "CREATE INDEX \"" + TableName + "_Idx_Key\" ON \"" + TableName + "\" (Key, Date DESC)"
            };
        }

        public void Clear ()
        {
            using (var db = Open ())
            {
                db.Execute ("DELETE FROM \"" + TableName + "\" ");
            }
        }

        public void Set (string key, T value, int count = -1, bool isDistinct = false)
        {
            using (var db = Open ())
            {
                using (var trans = db.BeginTransaction ())
                {
                    InsertItem (key, Newtonsoft.Json.JsonConvert.SerializeObject (value), count, isDistinct, trans, db);
                    trans.Commit ();
                }
            }
        }

        public void Set (IEnumerable<KeyValuePair<string, T>> items, int count = -1, bool isDistinct = false)
        {
            using (var db = Open ())
            {
                using (var trans = db.BeginTransaction ())
                {
                    foreach (var i in items)
                    {
                        InsertItem (i.Key, Newtonsoft.Json.JsonConvert.SerializeObject (i.Value), count, isDistinct, trans, db);
                    }
                    trans.Commit ();
                }
            }
        }

        private void InsertItem (string key, string value, int count, bool isDistinct, SQLiteTransaction trans, SQLiteConnection db)
        {
            var info = new { Date = DateTime.UtcNow, Key = key, Value = value, Count = count };
            if (isDistinct)
            {
                db.Execute ("Delete From \"" + TableName + "\" Where Key = @Key And Value = @Value", info, trans);
            }
            db.Execute ("INSERT INTO \"" + TableName + "\" (Date, Key, Value) values (@Date, @Key, @Value)", info, trans);
            if (count > 0)
            {
                db.Execute ("Delete from \"" + TableName + "\" Where Id in (Select Id FROM \"" + TableName + "\" Where Key = @Key Order by Key, Date DESC Limit 100000 Offset @Count)",
                    info, trans);
            }
        }

        public void Remove (string key, DateTime olderThan)
        {
            using (var db = Open ())
            {
                db.Execute ("Delete FROM \"" + TableName + "\" Where Key = @Key AND Date <= @olderThan", new { Key = key, olderThan = olderThan.ToUniversalTime () });
            }
        }

        public void Remove (string key, T item)
        {
            using (var db = Open ())
            {
                db.Execute ("Delete FROM \"" + TableName + "\" Where Key = @Key AND Value = @Value", new { Key = key, Value = Newtonsoft.Json.JsonConvert.SerializeObject (item) });
            }
        }

        public void Remove (string key)
        {
            using (var db = Open ())
            {
                db.Execute ("Delete FROM \"" + TableName + "\" Where Key = @Key ", new { Key = key });
            }
        }

        public void Remove (DateTime olderThan)
        {
            using (var db = Open ())
            {
                db.Execute ("DELETE FROM \"" + TableName + "\" Where Date <= @olderThan", new { olderThan = olderThan.ToUniversalTime () });
            }
        }

        public IEnumerable<T> Get (string key, bool sortDescendingByDate = true)
        {
            // prepare SQL
            string query;
            if (sortDescendingByDate)
                query = "Select Value FROM \"" + TableName + "\" Where Key = @Key Order by Key, Date DESC";
            else
                query = "Select Value FROM \"" + TableName + "\" Where Key = @Key Order by Id";
            // execute query
            using (var db = Open ())
            {
                foreach (var item in db.Query<string> (query, new { Key = key }, null, false))
                    yield return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (item);
            }
        }

        public IEnumerable<T> Get (bool sortDescendingByDate = true)
        {
            // prepare SQL
            string query;
            if (sortDescendingByDate)
                query = "Select Value FROM \"" + TableName + "\" Order by Id DESC";
            else
                query = "Select Value FROM \"" + TableName + "\" Order by Id";
            // execute query
            using (var db = Open ())
            {
                foreach (var item in db.Query<string> (query, null, null, false))
                    yield return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (item);
            }
        }

        public IEnumerable<T> Get (IEnumerable<string> keys, bool sortDescendingByDate = true)
        {
            // prepare SQL
            string query;
            if (sortDescendingByDate)
                query = "Select Value FROM \"" + TableName + "\" Where Key in @Keys Order by Id DESC";
            else
                query = "Select Value FROM \"" + TableName + "\" Where Key in @Keys Order by Id";
            // execute query
            using (var db = Open ())
            {
                foreach (var item in db.Query<string> (query, new { Keys = keys }, null, false))
                    yield return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (item);
            }
        }

        public IEnumerable<T> Find (string key, object searchParam, int limit = 100, bool sortDescendingByDate = true)
        {
            // prepare SQL
            string query;
            if (sortDescendingByDate)
                query = "Select Value FROM \"" + TableName + "\" Where Key = @Key AND Value LIKE @value Order by Key, Date DESC Limit @limit";
            else
                query = "Select Value FROM \"" + TableName + "\" Where Key = @Key AND Value LIKE @value Order by Id Limit @limit";
            // execute query
            using (var db = Open ())
            {
                return db.Query<string> (query, new { Key = key, limit = limit, value = Newtonsoft.Json.JsonConvert.SerializeObject (searchParam).Replace ('{', '%').Replace ('}', '%').Replace (',', '%') })
                    .Select (i => Newtonsoft.Json.JsonConvert.DeserializeObject<T> (i));
            }
        }

        public IEnumerable<T> Find (object searchParam, int limit = 100, bool sortDescendingByDate = true)
        {
            // prepare SQL
            string query;
            if (sortDescendingByDate)
                query = "Select Value FROM \"" + TableName + "\" Where Value LIKE @value Order by Key, Date DESC Limit @limit";
            else
                query = "Select Value FROM \"" + TableName + "\" Where Value LIKE @value Order by Id Limit @limit";
            // execute query
            using (var db = Open ())
            {
                return db.Query<string> (query, new { limit = limit, value = Newtonsoft.Json.JsonConvert.SerializeObject (searchParam).Replace ('{', '%').Replace ('}', '%').Replace (',', '%') })
                    .Select (i => Newtonsoft.Json.JsonConvert.DeserializeObject<T> (i));
            }
        }

        public void FindAndRemove (string key, object searchParam)
        {
            using (var db = Open ())
            {
                db.Execute ("Delete FROM \"" + TableName + "\" Where Key = @Key AND Value LIKE @value ", new { Key = key, value = Newtonsoft.Json.JsonConvert.SerializeObject (searchParam).Replace ('{', '%').Replace ('}', '%').Replace (',', '%') });
            }
        }
    }
}