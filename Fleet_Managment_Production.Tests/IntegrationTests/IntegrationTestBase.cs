using Fleet_Managment_Production.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace Fleet_Managment_Production.Tests.IntegrationTests
{
    public abstract class IntegrationTestBase : IDisposable
    {
        private readonly SqliteConnection _connection;
        protected readonly AppDbContext _context;

        protected IntegrationTestBase()
        {
            SQLitePCL.Batteries.Init();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new AppDbContext(options);

            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
            }
        }
    }
}