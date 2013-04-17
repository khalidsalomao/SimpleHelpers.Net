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
    /// <summary>
    /// Simple storage using sqlite.
    /// </summary>
    public class SQLiteStorage<T> where T : class
    {
        protected const int cacheSize = 1000;
        
        protected string m_connectionString = null; 
        
        protected SQLiteStorageOptions defaultOptions = null;

        public string TableName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteStorage" /> class.
        /// Uses SQLiteStorageOptions.UniqueKeys () as default options.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public SQLiteStorage (string filename) : this (filename, typeof (T).Name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteStorage" /> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="count">The count.</param>
        /// <param name="isDistinct">The is distinct.</param>
        public SQLiteStorage (string filename, int count, bool isDistinct)
            : this (filename, typeof (T).Name, new SQLiteStorageOptions { MaximumItemsPerKeys = count, OverwriteSimilarItems = isDistinct})
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteStorage" /> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="options">The options.</param>
        public SQLiteStorage (string filename, SQLiteStorageOptions options) : this (filename, typeof (T).Name, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteStorage" /> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="options">The options.</param>
        public SQLiteStorage (string filename, string tableName, SQLiteStorageOptions options)
        {
            if (String.IsNullOrEmpty (tableName))
                throw new ArgumentNullException ("TableName");
            defaultOptions = options ?? SQLiteStorageOptions.UniqueKeys ();
            TableName = tableName;
            Configure (filename, cacheSize);
        }

        /// <summary>
        /// Default behavior of how SQLiteStorage store items.
        /// </summary>
        public SQLiteStorageOptions DefaultOptions
        {
            get { return defaultOptions; }
            set
            {                
                if (value == null) throw new ArgumentNullException("DefaultOptions"); 
                defaultOptions = value;
            }
        }

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
            using (var connection = Open ())
            {
                if (connection.Query<Int64> ("SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@table", new { table = TableName }).FirstOrDefault () == 0)
                {
                    foreach (var sql in GetTableCreateSQL ())
                    {
                        connection.Execute (sql);
                    }
                }
            }
        }
        
        /// <summary>
        /// Helper method to optimize the sqlite file.
        /// </summary>
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

        /// <summary>
        /// Clears all stored items.
        /// </summary>
        public void Clear ()
        {
            using (var db = Open ())
            {
                db.Execute ("DELETE FROM \"" + TableName + "\" ");
            }
        }

        /// <summary>
        /// Stores an item with an associated key.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="value">Item to be stored.</param>
        public void Set (string key, T value)
        {
            using (var db = Open ())
            {
                using (var trans = db.BeginTransaction ())
                {
                    insertInternal (key, Newtonsoft.Json.JsonConvert.SerializeObject (value), DefaultOptions.MaximumItemsPerKeys, DefaultOptions.OverwriteSimilarItems, trans, db);
                    trans.Commit ();
                }
            }
        }

        /// <summary>
        /// Stores a list of items.
        /// </summary>
        /// <param name="items">List of items.</param>
        public void Set (IEnumerable<KeyValuePair<string, T>> items)
        {
            int counter = 0;
            using (var db = Open ())
            {
                var trans = db.BeginTransaction ();
                try
                {
                    foreach (var i in items)
                    {
                        insertInternal (i.Key, Newtonsoft.Json.JsonConvert.SerializeObject (i.Value), DefaultOptions.MaximumItemsPerKeys, DefaultOptions.OverwriteSimilarItems, trans, db);
                        if (++counter % 2500 == 0)
                        {
                            trans.Commit ();
                            trans = db.BeginTransaction ();
                        }
                    }
                    trans.Commit ();
                }
                finally
                {
                    trans.Dispose ();
                }
            }
        }

        /// <summary>
        /// Stores an item with custom options overridins the default options.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="value">Item to be stored.</param>
        /// <param name="count">
        /// The maximum number of items per key.
        /// Use '1' to unique keys.
        /// Use '0' to allow unlimited items per key.
        /// </param>
        /// <param name="isDistinct">
        /// If should try to remove similar items with the same key.
        /// Similar items are detected by equality of serialized object string.
        /// </param>
        public void SetSpecial (string key, T value, int count, bool isDistinct = false)
        {
            using (var db = Open ())
            {
                using (var trans = db.BeginTransaction ())
                {
                    insertInternal (key, Newtonsoft.Json.JsonConvert.SerializeObject (value), count, isDistinct, trans, db);
                    trans.Commit ();
                }
            }
        }

        private void insertInternal (string key, string value, int count, bool isDistinct, SQLiteTransaction trans, SQLiteConnection db)
        {
            var info = new { Date = DateTime.UtcNow, Key = key, Value = value, Count = count };
            if (isDistinct && count != 1)
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

        /// <summary>
        /// Removes all items associated with the specified key and date range.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="olderThan">The older than.</param>
        public void Remove (string key, DateTime olderThan)
        {
            using (var db = Open ())
            {
                db.Execute ("Delete FROM \"" + TableName + "\" Where Key = @Key AND Date <= @olderThan", new { Key = key, olderThan = olderThan.ToUniversalTime () });
            }
        }

        /// <summary>
        /// Removes all items associated with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        public void Remove (string key)
        {
            using (var db = Open ())
            {
                db.Execute ("Delete FROM \"" + TableName + "\" Where Key = @Key ", new { Key = key });
            }
        }

        /// <summary>
        /// Removes all items associated with the specified keys.
        /// </summary>
        /// <param name="keys">The list of keys.</param>
        public void Remove (IEnumerable<string> keys)
        {
            using (var db = Open ())
            {
                db.Execute ("Delete FROM \"" + TableName + "\" Where Key IN @Key ", new { Key = keys });
            }
        }

        /// <summary>
        /// Removes all items by date range.
        /// </summary>
        /// <param name="olderThan">The older than.</param>
        public void Remove (DateTime olderThan)
        {
            using (var db = Open ())
            {
                db.Execute ("DELETE FROM \"" + TableName + "\" Where Date <= @olderThan", new { olderThan = olderThan.ToUniversalTime () });
            }
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove (SQLiteStorageItem<T> item)
        {
            using (var db = Open ())
            {
                db.Execute ("DELETE FROM \"" + TableName + "\" Where Id = @Id", item);
            }
        }

        /// <summary>
        /// Gets items associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<T> Get (string key, bool sortNewestFirst = true)
        {
            return getInternal (key, sortNewestFirst);
        }

        /// <summary>
        /// Gets all items.
        /// </summary>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<T> Get (bool sortNewestFirst = true)
        {
            return getInternal (null, sortNewestFirst);
        }

        /// <summary>
        /// Gets items associated with the specified keys.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<T> Get (IEnumerable<string> keys, bool sortNewestFirst = true)
        {
            return getInternal (keys, sortNewestFirst);
        }
        
        private IEnumerable<T> getInternal (object key, bool sortNewestFirst = true)
        {
            // prepare SQL
            System.Text.StringBuilder query = new System.Text.StringBuilder ("Select Value FROM \"", 50).Append (TableName).Append ('\"');
            // create filter
            if (key == null)
            {
                if ((key as System.Collections.IEnumerable) != null)
                    query.Append ("WHERE Key IN @Key");
                else
                    query.Append ("WHERE Key = @Key");
            }
            // create sort order
            if (sortNewestFirst)
                query.Append (" Order by Id DESC");
            else
                query.Append (" Order by Id");
            // execute query
            using (var db = Open ())
            {
                foreach (var item in db.Query<string> (query.ToString (), new { Key = key }, null, false))
                    yield return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (item);
            }
        }

        /// <summary>
        /// Gets the stored items with its details.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<SQLiteStorageItem<T>> GetDetails (string key, bool sortNewestFirst = true)
        {
            return getDetailsInternal (key, sortNewestFirst);
        }

        /// <summary>
        /// Gets the stored items with its details.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<SQLiteStorageItem<T>> GetDetails (IEnumerable<string> keys, bool sortNewestFirst = true)
        {
            return getDetailsInternal (keys, sortNewestFirst);
        }

        /// <summary>
        /// Gets the stored items with its details.
        /// </summary>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<SQLiteStorageItem<T>> GetDetails (bool sortNewestFirst = true)
        {
            return getDetailsInternal (null, sortNewestFirst);
        }

        private IEnumerable<SQLiteStorageItem<T>> getDetailsInternal (object key, bool sortNewestFirst = true)
        {
            // prepare SQL
            System.Text.StringBuilder query = new System.Text.StringBuilder ("Select * FROM \"", 50).Append (TableName).Append ('\"');
            // create filter
            if (key == null)
            {
                if ((key as System.Collections.IEnumerable) != null)
                    query.Append ("WHERE Key IN @Key");
                else
                    query.Append ("WHERE Key = @Key");
            }            
            // create sort order            
            if (sortNewestFirst)
                query.Append (" Order by Id DESC");
            else
                query.Append (" Order by Id");
            // execute query
            using (var db = Open ())
            {
                foreach (var item in db.Query<SQLiteStorageItem<T>> (query.ToString (), new { Key = key }, null, false))
                    yield return item;
            }
        }

        /// <summary>
        /// Finds all items that meets the search parameters.
        /// Uses a LIKE sql query to locate items.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<T> FindByField (string key, string fieldName, object fieldValue, bool sortNewestFirst = true)
        {
            // prepare SQL
            string query;
            string keyFilter = key == null ? "" : "Key = @Key";            
            if (sortNewestFirst)
                query = "Select Value FROM \"" + TableName + "\" Where " + keyFilter + " AND Value LIKE @value Order by Id DESC";
            else
                query = "Select Value FROM \"" + TableName + "\" Where " + keyFilter + " AND Value LIKE @value Order by Id";
            // execute query
            using (var db = Open ())
            {
                return db.Query<string> (query, new { Key = key, value = prepareSearchParam (fieldName, fieldValue) })
                    .Select (i => Newtonsoft.Json.JsonConvert.DeserializeObject<T> (i));
            }
        }

        /// <summary>
        /// Finds all items that meets the search parameters.
        /// Uses a LIKE sql query to locate items.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<T> FindByField (string fieldName, object fieldValue, bool sortNewestFirst = true)
        {
            return FindByField (null, fieldName, fieldValue, sortNewestFirst);
        }

        /// <summary>
        /// Removes all items that meets the search parameters.
        /// Uses a LIKE sql query to locate items.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="parameters">The search parameters.</param>
        public void FindAndRemove (string key, object parameters)
        {
            string whereClause;
            object queryParameter;
            prepareWhereClause (key, parameters, null, out whereClause, out queryParameter);

            string query = "Delete FROM \"" + TableName + "\"" + whereClause;

            using (var db = Open ())
            {
                db.Execute (query.ToString (), queryParameter);
            }
        }

        /// <summary>
        /// Finds all items that meets the search parameters.
        /// Uses a LIKE sql query to locate items.
        /// </summary>
        /// <param name="parameters">The search parameters.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<T> Find (object parameters, bool sortNewestFirst = true)
        {
            return Find (null, parameters, sortNewestFirst);
        }

        /// <summary>
        /// Finds all items that meets the search parameters.
        /// Uses a LIKE sql query to locate items.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="parameters">The search parameters.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<T> Find (string key, object parameters, bool sortNewestFirst = true)
        {
            string whereClause;
            object queryParameter;
            prepareWhereClause (key, parameters, sortNewestFirst, out whereClause, out queryParameter);
            // prepare SQL
            string query = "Select Value FROM \"" + TableName + "\"" + whereClause;
            // execute query
            using (var db = Open ())
            {
                foreach (var i in db.Query<string> (query, queryParameter, null, false))
                    yield return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (i);
            }
        }

        static void prepareWhereClause (string key, object parameters, bool? sortDescendingByDate, out string whereClause, out object queryParameter)
        {
            System.Text.StringBuilder query = new System.Text.StringBuilder (" WHERE", 50);
            if (key != null)
                query.Append (" Key = @Key");            
            if (parameters != null)
            {
                System.ComponentModel.PropertyDescriptorCollection properties = System.ComponentModel.TypeDescriptor.GetProperties (parameters);

                var values = new List<KeyValuePair<string, object>> (properties.Count + 1);
                values.Add (new KeyValuePair<string, object> ("Key", key));

                int i = 0;
                foreach (System.ComponentModel.PropertyDescriptor property in properties)
                {
                    ++i;
                    object value = property.GetValue (parameters);                    
                    string vName = "v" + i;
                    if (i > 1 || key != null)
                        query.Append (" AND");
                    query.Append (" Value LIKE @").Append (vName);
                    values.Add (new KeyValuePair<string, object> (vName, "%\"" + property.Name + "\":" + Newtonsoft.Json.JsonConvert.SerializeObject (value) + "%"));
                }
                queryParameter = createAnonymousType (values);
            }
            else 
            {
                queryParameter = new { Key = key };
            }
            if (sortDescendingByDate.HasValue)
            {
                if (sortDescendingByDate.Value)
                    query.Append (" Order by Date DESC");
                else
                    query.Append (" Order by Id");
            }
            whereClause = query.ToString ();            
        }

        static string prepareSearchParam (string fieldName, object fieldValue)
        {
            return "%\"" + fieldName + "\":" + Newtonsoft.Json.JsonConvert.SerializeObject (fieldValue) + "%";
        }

        static object createAnonymousType (IEnumerable<KeyValuePair<string, object>> dict)
        {
            var eo = new System.Dynamic.ExpandoObject ();
            var eoColl = (ICollection<KeyValuePair<string, object>>)eo;
            foreach (KeyValuePair<string, object> kvp in dict)
            {
                eoColl.Add (kvp);
            }
            return (dynamic)eo;
        }
    }
    
    /// <summary>
    /// SQLiteStorage options
    /// </summary>
    public class SQLiteStorageOptions
    {
        private bool m_allowDuplicatedKeys;

        private int m_maximumItemsPerKeys;

        private bool m_overwriteSimilarItems;

        /// <summary>
        /// If the SQLiteStorage instance should allow duplicated keys.
        /// </summary>
        public bool AllowDuplicatedKeys
        {
            get { return m_allowDuplicatedKeys; }
            set
            {
                if (!value)
                {
                    m_maximumItemsPerKeys = 1;
                    m_overwriteSimilarItems = false;
                }
                m_allowDuplicatedKeys = value;
            }
        }

        /// <summary>
        /// If AllowDuplicatedKeys is enabled, the maximum number of items per key.
        /// Use '1' to unique keys.
        /// Use '0' to allow unlimited items per key.
        /// </summary>
        public int MaximumItemsPerKeys
        {
            get { return m_maximumItemsPerKeys; }
            set
            {                
                m_maximumItemsPerKeys = value;
                m_allowDuplicatedKeys = value != 1;
            }
        }

        /// <summary>
        /// If AllowDuplicatedKeys is enabled, will try to remove similar items with the same key.
        /// Similar items are detected by equality of serialized object string.
        /// </summary>
        /// <value>The overwrite similar items.</value>
        public bool OverwriteSimilarItems
        {
            get { return m_overwriteSimilarItems; }
            set { m_overwriteSimilarItems = value; }
        }

        /// <summary>
        /// Uniques the keys allow only one stored item per key.
        /// </summary>
        public static SQLiteStorageOptions UniqueKeys ()
        {
            return new SQLiteStorageOptions { AllowDuplicatedKeys = false };
        }

        /// <summary>
        /// Keep an history of items by keeping an unlimited number of stored items per key.
        /// </summary>
        public static SQLiteStorageOptions KeepItemsHistory ()
        {
            return new SQLiteStorageOptions { AllowDuplicatedKeys = true, MaximumItemsPerKeys = -1, OverwriteSimilarItems = false };
        }

        /// <summary>
        /// Keep an history of items by keeping an unlimited number of stored items per key but removing similar items.
        /// </summary>
        /// <param name="maxItemsPerKeys">The maximum number of items per key.</param>
        public static SQLiteStorageOptions KeepUniqueItems (int maxItemsPerKeys = -1)
        {
            return new SQLiteStorageOptions { AllowDuplicatedKeys = true, MaximumItemsPerKeys = maxItemsPerKeys, OverwriteSimilarItems = true };
        }
    }

    /// <summary>
    /// Item details used in the sqlite storage.
    /// </summary>
    public class SQLiteStorageItem<T> where T : class
    {
        private string m_value;
        
        private T m_item = null;

        /// <summary>
        /// Internal Id in database.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Date when the item was stored in database (UTC).
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// The key associated with this Item.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Value is the json representation of the stored item.
        /// </summary>
        public string Value
        {
            get { return m_value; }
            set
            {
                // clear stored item instance
                if (m_value != value)
                    m_item = null;
                // set value
                m_value = value;
            }
        }

        /// <summary>
        /// The stored Item.
        /// </summary>
        public T Item
        {
            get
            {
                if (m_item == null)
                    m_item = Value == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<T> (Value);
                return m_item;
            }
            set
            {
                Value = Newtonsoft.Json.JsonConvert.SerializeObject (value);
            }
        }
    }
}