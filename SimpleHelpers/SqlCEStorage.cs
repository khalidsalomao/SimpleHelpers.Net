#region *   License     *
/*
    SimpleHelpers - SqlCEStorage   

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
using System.Data.SqlServerCe;
using System.Linq;
using Dapper;

namespace SimpleHelpers.SQLCE
{    
    /// <summary>
    /// Simple key value storage using sqlce.
    /// All member methods are thread-safe, so any instance can be safely be accessed by multiple threads.
    /// All stored items are serialized to json by json.net.
    /// Note: this nuget package contains C# source code and depends on .Net 4.0.
    /// </summary>    
    /// <example>
    /// // create a new instance
    /// SqlCEStorage db = new SqlCEStorage ("path_to_my_file.sqlite", SqlCEStorageOptions.UniqueKeys ());
    /// // save an item
    /// db.Set ("my_key_for_this_item", new My_Class ());
    /// // get it back
    /// var my_obj = db.Get ("my_key_for_this_item").FirstOrDefault ();    
    /// </example>
    public class SqlCEStorage<T> where T : class
    {
        protected const int cacheSize = 1000;
        
        protected string m_connectionString = null; 
        
        protected SqlCEStorageOptions defaultOptions = null;

        public string TableName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlCEStorage" /> class.
        /// Uses SqlCEStorageOptions.UniqueKeys () as default options.
        /// </summary>
        /// <param name="filename">The filename.</param>
        public SqlCEStorage (string filename) : this (filename, typeof (T).Name, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlCEStorage" /> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="count">The count.</param>
        /// <param name="isDistinct">The is distinct.</param>
        public SqlCEStorage (string filename, int count, bool isDistinct)
            : this (filename, typeof (T).Name, new SqlCEStorageOptions { MaximumItemsPerKeys = count, OverwriteSimilarItems = isDistinct })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlCEStorage" /> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="options">The options.</param>
        public SqlCEStorage (string filename, SqlCEStorageOptions options)
            : this (filename, typeof (T).Name, options)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlCEStorage" /> class.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="options">The options.</param>
        public SqlCEStorage (string filename, string tableName, SqlCEStorageOptions options)
        {
            if (String.IsNullOrEmpty (tableName))
                throw new ArgumentNullException ("TableName");
            defaultOptions = options ?? SqlCEStorageOptions.UniqueKeys ();
            TableName = tableName;
            Configure (filename, cacheSize);
        }

        /// <summary>
        /// Default behavior of how SqlCEStorage store items.
        /// </summary>
        public SqlCEStorageOptions DefaultOptions
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
            var sb = new SqlCeConnectionStringBuilder ();
            sb.DataSource = filename;
            m_connectionString = sb.ToString ();
            if (!System.IO.File.Exists (filename))
            {
                using (SqlCeEngine db = new SqlCeEngine (m_connectionString))
                    db.CreateDatabase ();
            }
            // execute initialization
            CreateTable ();
        }
        SqlCeConnection connection = null;

        protected SqlCeConnection Open ()
        {
            if (m_connectionString == null)
            {
                throw new ArgumentNullException ("Invalid connection string, call Configure to set the connection string.");
            }
            if (connection == null || connection.State == System.Data.ConnectionState.Closed)
            {
                connection = new SqlCeConnection (m_connectionString);
                connection.Open ();
            }
            return connection;
        }

        public void Close ()
        {
            Open ().Close ();            
        }

        protected void CreateTable ()
        {
            var connection = Open ();
            if (connection.Query<int> ("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@table", new { table = TableName }).FirstOrDefault () == 0)
            {
                foreach (var sql in GetTableCreateSQL ())
                {
                    connection.Execute (sql);
                }
            }
        }
        
        /// <summary>
        /// Helper method to optimize the sqlce file.
        /// </summary>
        public void Shrink ()
        {            
            using (var db = new SqlCeEngine (m_connectionString))
            {
                db.Shrink ();
            }
        }
        
        protected string[] GetTableCreateSQL ()
        {
            return new string[]
            {
                "CREATE TABLE  \"" + TableName + "\" ([Id] bigint IDENTITY NOT NULL PRIMARY KEY, " +
                "[Date] datetime NOT NULL, " +
                "[Key] nchar (256) NOT NULL," +
                "[Value] ntext NOT NULL)",
                "CREATE INDEX \"" + TableName + "_Idx_Key\" ON \"" + TableName + "\" ([Key], [Date] DESC)"
            };
        }

        /// <summary>
        /// Clears all stored items.
        /// </summary>
        public void Clear ()
        {
            var db = Open ();
            db.Execute ("DELETE FROM \"" + TableName + "\" ");
        }

        /// <summary>
        /// Stores an item with an associated key.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        /// <param name="value">Item to be stored.</param>
        public void Set (string key, T value)
        {
            var db = Open ();
            using (var trans = db.BeginTransaction ())
            {
                insertInternal (key, Newtonsoft.Json.JsonConvert.SerializeObject (value), DefaultOptions.MaximumItemsPerKeys, DefaultOptions.OverwriteSimilarItems, trans, db);
                trans.Commit ();
            }                    
         }

        /// <summary>
        /// Stores a list of items.
        /// </summary>
        /// <param name="items">List of items.</param>
        public void Set (IEnumerable<KeyValuePair<string, T>> items)
        {
            int counter = 0;
            var db = Open ();
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
            var db = Open ();
            using (var trans = db.BeginTransaction ())
            {
                insertInternal (key, Newtonsoft.Json.JsonConvert.SerializeObject (value), count, isDistinct, trans, db);
                trans.Commit ();
            }
        }

        private void insertInternal (string key, string value, int count, bool isDistinct, SqlCeTransaction trans, SqlCeConnection db)
        {
            var info = new { Date = DateTime.UtcNow, Key = key, Value = value, Count = count };
            // removal of similar item
            if (isDistinct && count != 1)
            {
                db.Execute ("Delete From \"" + TableName + "\" Where [Key] = @Key And [Value] = @Value", info, trans);
            }
            // insert item
            db.Execute ("INSERT INTO \"" + TableName + "\" ([Date], [Key], [Value]) values (@Date, @Key, @Value)", info, trans);
            // removal of history items
            if (count > 0)
            {
                db.Execute ("Delete from \"" + TableName + "\" Where [Id] in (Select [Id] FROM \"" + TableName + "\" Where [Key] = @Key Order by [Key], [Date] DESC OFFSET @Count ROWS FETCH NEXT 10000 ROWS ONLY)",
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
            Open ().Execute ("Delete FROM \"" + TableName + "\" Where [Key] = @Key AND [Date] <= @olderThan", new { Key = key, olderThan = olderThan.ToUniversalTime () });
        }

        /// <summary>
        /// Removes all items associated with the specified key.
        /// </summary>
        /// <param name="key">The key associated with the item.</param>
        public void Remove (string key)
        {
            Open ().Execute ("Delete FROM \"" + TableName + "\" Where [Key] = @Key ", new { Key = key });
        }

        /// <summary>
        /// Removes all items associated with the specified keys.
        /// </summary>
        /// <param name="keys">The list of keys.</param>
        public void Remove (IEnumerable<string> keys)
        {
            Open ().Execute ("Delete FROM \"" + TableName + "\" Where [Key] IN @Key ", new { Key = keys });
        }

        /// <summary>
        /// Removes all items by date range.
        /// </summary>
        /// <param name="olderThan">The older than.</param>
        public void Remove (DateTime olderThan)
        {
            Open ().Execute ("DELETE FROM \"" + TableName + "\" Where [Date] <= @olderThan", new { olderThan = olderThan.ToUniversalTime () });
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove (SqlCEStorageItem<T> item)
        {
            Open ().Execute ("DELETE FROM \"" + TableName + "\" Where [Id] = @Id", item);
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">Internal item Id.</param>
        public void Remove (Int64 id)
        {
            using (var db = Open ())
            {
                db.Execute ("DELETE FROM \"" + TableName + "\" Where [Id] = @Id", new { Id = id });
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
            string query;
            object parameter;
            prepareGetSqlQuery (key, true, sortNewestFirst, out query, out parameter);
            // execute query
            var db = Open ();
            foreach (var item in db.Query<string> (query.ToString (), parameter, null, false))
                yield return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (item);
        }

        /// <summary>
        /// Gets the stored items with its details.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<SqlCEStorageItem<T>> GetDetails (string key, bool sortNewestFirst = true)
        {
            return getDetailsInternal (key, sortNewestFirst);
        }

        /// <summary>
        /// Gets the stored items with its details.
        /// </summary>
        /// <param name="keys">The keys.</param>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<SqlCEStorageItem<T>> GetDetails (IEnumerable<string> keys, bool sortNewestFirst = true)
        {
            return getDetailsInternal (keys, sortNewestFirst);
        }

        /// <summary>
        /// Gets the stored items with its details.
        /// </summary>
        /// <param name="sortNewestFirst">The sort newest first.</param>
        public IEnumerable<SqlCEStorageItem<T>> GetDetails (bool sortNewestFirst = true)
        {
            return getDetailsInternal (null, sortNewestFirst);
        }

        private IEnumerable<SqlCEStorageItem<T>> getDetailsInternal (object key, bool sortNewestFirst = true)
        {
            // prepare SQL
            string query;
            object parameter;            
            prepareGetSqlQuery (key, false, sortNewestFirst, out query, out parameter);
            // execute query
            var db = Open ();
            foreach (var item in db.Query<SqlCEStorageItem<T>> (query.ToString (), parameter, null, false))
                yield return item;
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
            string keyFilter = key == null ? "" : "[Key] = @Key";            
            if (sortNewestFirst)
                query = "Select [Value] FROM \"" + TableName + "\" Where " + keyFilter + " AND [Value] LIKE @value Order by [Id] DESC";
            else
                query = "Select [Value] FROM \"" + TableName + "\" Where " + keyFilter + " AND [Value] LIKE @value Order by [Id]";
            // execute query
            var db = Open ();
            return db.Query<string> (query, new { Key = key, value = prepareSearchParam (fieldName, fieldValue) })
                    .Select (i => Newtonsoft.Json.JsonConvert.DeserializeObject<T> (i));
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
            prepareFindSqlQuery (key, parameters, null, out whereClause, out queryParameter);

            string query = "Delete FROM \"" + TableName + "\"" + whereClause;

            var db = Open ();
            db.Execute (query.ToString (), queryParameter);
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
            // prepare SQL
            string query;
            object queryParameter;
            prepareFindSqlQuery (key, parameters, sortNewestFirst, out query, out queryParameter);            
            // execute query
            var db = Open ();
            foreach (var i in db.Query<string> (query, queryParameter, null, false))
                yield return Newtonsoft.Json.JsonConvert.DeserializeObject<T> (i);
        }

        private void prepareGetSqlQuery (object key, bool selectValueOnly, bool? sortDescendingByDate, out string sqlQuery, out object parameters)
        {
            System.Text.StringBuilder query = new System.Text.StringBuilder ("Select ", 100);
            if (selectValueOnly)
                query.Append (" [Value] FROM \"");
            else
                query.Append (" * FROM \"");
            query.Append (TableName).Append ('\"');            
            // create filter
            if (key != null)
            {
                if ((key as string) != null)
                {
                    query.Append (" WHERE [Key] = @Key");
                    parameters = new { Key = (string)key };
                }
                else if ((key as System.Collections.IEnumerable) != null)
                {
                    query.Append (" WHERE [Key] IN @Key");
                    parameters = new { Key = (System.Collections.IEnumerable)key };
                }
                else
                {
                    parameters = null;
                }
            }
            else
            {
                parameters = null;
            }
            // create sort order
            if (sortDescendingByDate.HasValue)
            {
                if (sortDescendingByDate.Value)
                {
                    if (key == null)
                        query.Append (" Order by [Id] DESC");
                    else
                        query.Append (" Order by [Key], [Date] DESC");
                }
                else
                {
                    query.Append (" Order by [Id]");
                }
            }
            sqlQuery = query.ToString ();
        }

        private void prepareFindSqlQuery (string key, object parameters, bool? sortDescendingByDate, out string whereClause, out object queryParameter)
        {
            // prepare sql where statement
            System.Text.StringBuilder query = new System.Text.StringBuilder ("Select [Value] FROM \"", 120).Append (TableName).Append ("\" WHERE");

            // prepare key filter
            if (key != null)
                query.Append (" [Key] = @Key");
            // prepare parameters filter
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
                    query.Append (" [Value] LIKE @").Append (vName);
                    values.Add (new KeyValuePair<string, object> (vName, "%\"" + property.Name + "\":" + Newtonsoft.Json.JsonConvert.SerializeObject (value) + "%"));
                }
                queryParameter = createAnonymousType (values);
            }
            else 
            {
                queryParameter = new { Key = key };
            }
            // prepare sort mode
            if (sortDescendingByDate.HasValue)
            {
                if (sortDescendingByDate.Value)
                {
                    if (key == null)
                        query.Append (" Order by [Id] DESC");
                    else
                        query.Append (" Order by [Key], [Date] DESC");
                }
                else
                {
                    query.Append (" Order by [Id]");
                }
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
    /// SqlCEStorage options
    /// </summary>
    public class SqlCEStorageOptions
    {
        private bool m_allowDuplicatedKeys;

        private int m_maximumItemsPerKeys;

        private bool m_overwriteSimilarItems;

        /// <summary>
        /// If the SqlCEStorage instance should allow duplicated keys.
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
        public static SqlCEStorageOptions UniqueKeys ()
        {
            return new SqlCEStorageOptions { AllowDuplicatedKeys = false };
        }

        /// <summary>
        /// Keep an history of items by keeping an unlimited number of stored items per key.
        /// </summary>
        public static SqlCEStorageOptions KeepItemsHistory ()
        {
            return new SqlCEStorageOptions { AllowDuplicatedKeys = true, MaximumItemsPerKeys = -1, OverwriteSimilarItems = false };
        }

        /// <summary>
        /// Keep an history of items by keeping an unlimited number of stored items per key but removing similar items.
        /// </summary>
        /// <param name="maxItemsPerKeys">The maximum number of items per key.</param>
        public static SqlCEStorageOptions KeepUniqueItems (int maxItemsPerKeys = -1)
        {
            return new SqlCEStorageOptions { AllowDuplicatedKeys = true, MaximumItemsPerKeys = maxItemsPerKeys, OverwriteSimilarItems = true };
        }
    }

    /// <summary>
    /// Item details used in the sqlite storage.
    /// </summary>
    public class SqlCEStorageItem<T> where T : class
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
                if (m_item == null && Value != null)
                    m_item = Newtonsoft.Json.JsonConvert.DeserializeObject<T> (Value);
                return m_item;
            }
            set
            {
                Value = Newtonsoft.Json.JsonConvert.SerializeObject (value);
                m_item = null;
            }
        }
    }
}