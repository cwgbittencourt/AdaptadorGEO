using AdaptadorGEO.Integration.EntityFrameworkCore;
using AdaptadorGEO.Sql;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace AdaptadorGEO.Tests.Integration;

[TestClass]
public class SqlFragmentEntityFrameworkExtensionsTests
{
    [TestMethod]
    public void ToDbParameters_copies_sql_fragment_parameters()
    {
        var fragment = new SqlFragment(
            "select 1",
            new[]
            {
                new SqlParameter("@p0", 10),
                new SqlParameter("@p1", null)
            });

        var connection = new FakeDbConnection();
        var parameters = fragment.ToDbParameters(connection);

        Assert.AreEqual(2, parameters.Length);
        Assert.AreEqual("@p0", parameters[0].ParameterName);
        Assert.AreEqual(10, parameters[0].Value);
        Assert.AreEqual("@p1", parameters[1].ParameterName);
        Assert.AreEqual(DBNull.Value, parameters[1].Value);
    }

    private sealed class FakeDbConnection : DbConnection
    {
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

        public override void ChangeDatabase(string? databaseName) => throw new NotSupportedException();

        public override void Close() { }

        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);

        public override string Database => "Fake";

        public override void Open() { }

        public override string DataSource => "Fake";

        public override string ServerVersion => "1";

        public override ConnectionState State => ConnectionState.Open;
    }

    private sealed class FakeDbCommand : DbCommand
    {
        private readonly FakeDbConnection _connection;

        public FakeDbCommand(FakeDbConnection connection) => _connection = connection;

        [AllowNull]
        protected override DbConnection DbConnection
        {
            get => _connection;
            set => throw new NotSupportedException();
        }

        protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();

        protected override DbTransaction? DbTransaction { get; set; }

        public override bool DesignTimeVisible { get; set; }

        public override UpdateRowSource UpdatedRowSource { get; set; }

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;

        public override int CommandTimeout { get; set; }

        public override CommandType CommandType { get; set; } = CommandType.Text;

        public override void Cancel() { }

        public override int ExecuteNonQuery() => throw new NotSupportedException();

        public override object? ExecuteScalar() => throw new NotSupportedException();

        public override void Prepare() { }

        protected override DbParameter CreateDbParameter() => new FakeDbParameter();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    private sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }

        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

        public override bool IsNullable { get; set; }

        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;

        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;

        public override object? Value { get; set; }

        public override bool SourceColumnNullMapping { get; set; }

        public override int Size { get; set; }

        public override void ResetDbType() { }
    }

    private sealed class FakeDbParameterCollection : DbParameterCollection
    {
        private readonly List<object> _items = new();

        public override int Add(object? value)
        {
            _items.Add(value!);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                _items.Add(value!);
            }
        }

        public override void Clear() => _items.Clear();

        public override bool Contains(object? value) => value is not null && _items.Contains(value);

        public override bool Contains(string? value) => value is not null && _items.OfType<DbParameter>().Any(parameter => parameter.ParameterName == value);

        public override void CopyTo(Array array, int index) => _items.ToArray().CopyTo(array, index);

        public override int Count => _items.Count;

        public override System.Collections.IEnumerator GetEnumerator() => _items.GetEnumerator();

        protected override DbParameter GetParameter(int index) => (DbParameter)_items[index];

        protected override DbParameter GetParameter(string parameterName) =>
            _items.OfType<DbParameter>().First(parameter => parameter.ParameterName == parameterName);

        public override int IndexOf(object? value) => value is null ? -1 : _items.IndexOf(value);

        public override int IndexOf(string? parameterName) =>
            parameterName is null
                ? -1
                : _items.FindIndex(item => item is DbParameter parameter && parameter.ParameterName == parameterName);

        public override void Insert(int index, object? value)
        {
            _items.Insert(index, value!);
        }

        public override bool IsFixedSize => false;

        public override bool IsReadOnly => false;

        public override bool IsSynchronized => false;

        public override void Remove(object? value)
        {
            if (value is not null)
            {
                _items.Remove(value);
            }
        }

        public override void RemoveAt(int index) => _items.RemoveAt(index);

        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _items.RemoveAt(index);
            }
        }

        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

        protected override void SetParameter(string? parameterName, DbParameter value)
        {
            if (parameterName is null)
            {
                throw new ArgumentNullException(nameof(parameterName));
            }

            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                _items[index] = value;
                return;
            }

            _items.Add(value);
        }

        public override object SyncRoot => this;
    }
}
