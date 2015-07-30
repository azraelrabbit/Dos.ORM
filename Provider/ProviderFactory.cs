﻿/*************************************************************************
 * 
 * Hxj.Data
 * 
 * 2010-2-10
 * 
 * steven hu   
 *  
 * Support: http://www.cnblogs.com/huxj
 *   
 * 
 * Change History:
 * 
 * 
**************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;

namespace Dos.ORM
{
    /// <summary>
    /// The db provider factory.
    /// </summary>
    public sealed class ProviderFactory
    {
        #region Private Members

        private static Dictionary<string, DbProvider> providerCache = new Dictionary<string, DbProvider>();

        private ProviderFactory() { }

        #endregion

        #region Public Members

        /// <summary>
        /// Creates the db provider.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="connectionString">The conn STR.</param>
        /// <returns>The db provider.</returns>
        public static DbProvider CreateDbProvider(string assemblyName, string className, string connectionString)
        {
            Check.Require(connectionString, "connectionString", Check.NotNullOrEmpty);

            if (connectionString.IndexOf("microsoft.jet.oledb", StringComparison.OrdinalIgnoreCase) > -1 || connectionString.IndexOf(".db3", StringComparison.OrdinalIgnoreCase) > -1)
            {
                Check.Require(connectionString.IndexOf("data source", StringComparison.OrdinalIgnoreCase) > -1, "ConnectionString的格式有错误，请查证！");

                string mdbPath = connectionString.Substring(connectionString.IndexOf("data source", StringComparison.OrdinalIgnoreCase) + "data source".Length + 1).TrimStart(' ', '=');
                if (mdbPath.ToLower().StartsWith("|datadirectory|"))
                {
                    mdbPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\App_Data" + mdbPath.Substring("|datadirectory|".Length);
                }
                else if (connectionString.StartsWith("./") || connectionString.EndsWith(".\\"))
                {
                    connectionString = connectionString.Replace("/", "\\").Replace(".\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
                }
                connectionString = connectionString.Substring(0, connectionString.ToLower().IndexOf("data source")) + "Data Source=" + mdbPath;
            }

            //如果是~则表示当前目录
            if (connectionString.Contains("~/") || connectionString.Contains("~\\"))
            {
                connectionString = connectionString.Replace("/", "\\").Replace("~\\", AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\') + "\\");
            }

            //by default, using sqlserver db provider
            if (string.IsNullOrEmpty(className))
            {
                className = typeof(SqlServer.SqlServerProvider).ToString();
            }
            else if (string.Compare(className, "System.Data.SqlClient", true) == 0 || string.Compare(className, "Dos.ORM.SqlServer", true) == 0)
            {
                className = typeof(SqlServer.SqlServerProvider).ToString();
            }
            else if (string.Compare(className, "Dos.ORM.SqlServer9", true) == 0 || className.IndexOf("SqlServer9", StringComparison.OrdinalIgnoreCase) >= 0 || className.IndexOf("sqlserver2005", StringComparison.OrdinalIgnoreCase) >= 0 || className.IndexOf("sql2005", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                className = typeof(SqlServer9.SqlServer9Provider).ToString();
            }
            else if (className.IndexOf("oracle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                className = typeof(Oracle.OracleProvider).ToString();
            }
            else if (className.IndexOf("access", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                className = typeof(MsAccess.MsAccessProvider).ToString();
            }
            else if (className.IndexOf("mysql", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                className = "Dos.ORM.MySql.MySqlProvider";
                assemblyName = "Dos.ORM.MySql";
            }
            else if (className.IndexOf("sqlite", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                className = "Dos.ORM.Sqlite.SqliteProvider";
                assemblyName = "Dos.ORM.Sqlite";
            }

            string cacheKey = string.Concat(assemblyName, className, connectionString);
            if (providerCache.ContainsKey(cacheKey))
            {
                return providerCache[cacheKey];
            }
            else
            {
                System.Reflection.Assembly ass;

                if (assemblyName == null)
                {
                    ass = typeof(DbProvider).Assembly;
                }
                else
                {
                    ass = System.Reflection.Assembly.Load(assemblyName);
                }

                DbProvider retProvider = ass.CreateInstance(className, false, System.Reflection.BindingFlags.Default, null, new object[] { connectionString }, null, null) as DbProvider;
                providerCache.Add(cacheKey, retProvider);
                return retProvider;
            }
        }

        /// <summary>
        /// Gets the default db provider.
        /// </summary>
        /// <value>The default.</value>
        public static DbProvider Default
        {
            get
            {
                try
                {
                    if (ConfigurationManager.ConnectionStrings.Count > 0)
                    {
                        DbProvider dbProvider;
                        ConnectionStringSettings connStrSetting = ConfigurationManager.ConnectionStrings[ConfigurationManager.ConnectionStrings.Count - 1];
                        string[] assAndClass = connStrSetting.ProviderName.Split(',');
                        if (assAndClass.Length > 1)
                        {
                            dbProvider = CreateDbProvider(assAndClass[1].Trim(), assAndClass[0].Trim(), connStrSetting.ConnectionString);
                        }
                        else
                        {
                            dbProvider = CreateDbProvider(null, assAndClass[0].Trim(), connStrSetting.ConnectionString);
                        }

                        dbProvider.ConnectionStringsName = connStrSetting.Name;

                        return dbProvider;
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Creates the db provider.
        /// </summary>
        /// <param name="connStrName">Name of the conn STR.</param>
        /// <returns>The db provider.</returns>
        public static DbProvider CreateDbProvider(string connStrName)
        {
            Check.Require(connStrName, "connStrName", Check.NotNullOrEmpty);

            DbProvider dbProvider;
            ConnectionStringSettings connStrSetting = ConfigurationManager.ConnectionStrings[connStrName];
            Check.Invariant(connStrSetting != null, null, new ConfigurationErrorsException(string.Concat("Cannot find specified connection string setting named as ", connStrName, " in application config file's ConnectionString section.")));
            string[] assAndClass = connStrSetting.ProviderName.Split(',');
            if (assAndClass.Length > 1)
            {
                dbProvider = CreateDbProvider(assAndClass[0].Trim(), assAndClass[1].Trim(), connStrSetting.ConnectionString);
            }
            else
            {
                dbProvider = CreateDbProvider(null, assAndClass[0].Trim(), connStrSetting.ConnectionString);
            }

            dbProvider.ConnectionStringsName = connStrName;

            return dbProvider;
        }

        #endregion
    }
}
