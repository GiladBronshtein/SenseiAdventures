using System;
using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace template.Server.Data
{
	public class DbRepository
	{
        private IDbConnection _dbConnection;

        public DbRepository(IConfiguration config)
        {
            _dbConnection = new SqliteConnection(config.GetConnectionString("DefaultConnection"));
        }


        public void OpenConnection()
        {
            //if there is no connection to the DB
            if (_dbConnection.State != ConnectionState.Open)
            {
                //Create new connection to the db
                _dbConnection.Open();
            }
        }

        public void CloseConnection()
        {
            _dbConnection.Close();
        }

        //parameters = query parameters
        public async Task<IEnumerable<T>> GetRecordsAsync<T>(string query, object parameters = null)
        {
            try
            {
                OpenConnection();

                if (parameters == null) parameters = new { };

                //Query - Method from dapper
                //Query<RETURN_TYPE>();
                IEnumerable<T> records = await _dbConnection.QueryAsync<T>(query, parameters, commandType: CommandType.Text);

                CloseConnection();
                return records;
            }
            catch (Exception ex)
            {
                CloseConnection();
                return null;
                throw;
            }
        }

        public async Task<int> SaveDataAsync(string query, object parameters = null)
        {
            try
            {
                OpenConnection();

                if (parameters == null) parameters = new { };

                //Return the amount of saved records 
                int records = await _dbConnection.ExecuteAsync(query, parameters, commandType: CommandType.Text);

                CloseConnection();

                //if one records or more updated - return true. else return false
                return records;
            }
            catch (Exception ex)
            {
                CloseConnection();
                //return 0;
                throw;
                //throw;
            }
        }

        public async Task<int> InsertReturnIdAsync(string query, object parameters = null)
        {
            try
            {
                OpenConnection();

                if (parameters == null) parameters = new { };

                int results = await _dbConnection.ExecuteAsync(sql: query, param: parameters, commandType: CommandType.Text);

                if (results > 0)
                {
                    int Id = _dbConnection.Query<int>("SELECT last_insert_rowid()").FirstOrDefault();
                    CloseConnection();
                    return Id;
                }
                CloseConnection();
                return 0;
            }
            catch (System.Exception)
            {
                CloseConnection();
                //return null;
                throw;

            }
        }

    }
}

