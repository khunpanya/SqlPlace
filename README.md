# SqlPlace

[![SqlPlace on Nuget](https://img.shields.io/nuget/vpre/SqlPlace.svg)](https://www.nuget.org/packages/SqlPlace)

SqlPlace is a .NET framework library to help you build complex parameterized SQL query.\
(.NET Standard version coming soon)

- Help you compose SQL and get the result with shortest code possible.
- Help you parameterize query with less code. Also free you from parameter ordering hassles.
- Help you compose complex query using statement template.
- Help you dynamically generate query from data list.
- Help you automatically switch among SQL dialects.
- Good for making SQL for dynamic CRUD, WHERE-IN clause, nested query, MERGE query, PIVOT query, etc.

---

- [Basic usage](#basic-usage)
- [Parameterizing](#parameterizing)
    - [Indexed parameters](#indexed-parameters)
    - [Named parameters](#named-parameters)
- [Templating](#templating)
    - [Nested statement](#nested-statement)
    - [List](#list)
    - [Predefined lists](#predefined-lists)
- [Helper extensions](#helper-extensions)
    - [Execution](#execution)
    - [Extract objects' properties](#extract-objects-properties)
- [Advance](#advance)
    - [Parameter info](#parameter-info)
    - [Other DB Providers](#other-db-providers)
- [SQL Dialect (Experimental feature)](#sql-dialect-experimental-feature)
    - [String implicit conversion](#string-implicit-conversion)
    - [SqlDialect](#sqldialect)

# Basic usage
Use **SqlPlace.SqlStatement** class to compose SQL and call **MakeCommand** to construct ADO.NET DbCommand.
```csharp
using(var conn = new SqlConnection(connString))
{
    // SqlCommand will be the default one. Change only if you want to use other DB provider.
    SqlPlace.SqlStatement.DefaultCommandFactory = new SqlPlace.Factories.SqlCommandFactory();

    var q = new SqlPlace.SqlStatement("select * from Table1 where f1={0}", 10);
    DbCommand cmd = q.MakeCommand(conn);

    // cmd is SqlCommand "select * from Table1 where f1=@p0"
    // with SqlParameter @p0 = 10
    // You can do any ADO.NET execution with it.
}
```

**Make** to get bare CommandText and Parameters that can be used with any other ORM.
```csharp
var q = new SqlStatement("select * from Table1 where f1={0}", 10);
CommandInfo cmdInfo = q.Make();

string commandText = cmdInfo.CommandText; // "select * from Table1 where f1=@p0"
string paramName0 = cmdInfo.Parameters[0].ParameterName; // "@p0"
object paramValue0 = cmdInfo.Parameters[0].Value; // 10
```

**MakeText** to get plain unparameterized SQL text.
```csharp
var q = new SqlStatement("select * from Table1 where f1={0} and f2={1} and f3={2}", 10, "A", new DateTime(1970, 1, 1));
string sql = q.MakeText(); // "select * from Table1 where f1=10 and f2='A' and f3='1970-01-01 00:00:00'"
```

# Parameterizing
## Indexed parameters
Use braces **{ }** with index numbers to place SQL parameters. Pretty much the same way as String.Format function.
```csharp
var q = new SqlStatement("select * from Table1 where f1={0} and f2={1}", 10, "A");
// This can constuct command text
// "select * from Table1 where f1=@p0 and f2=@p1"
// With parameters @p0 = 10, @p1 = 'A'
```

Placing parameters at the lines after is also allowed.
```csharp
var q = new SqlStatement("select * from Table1 where f1={0} and f2={1}");
q.PlaceParameter(0, 10);
q.PlaceParameter(1, "A");
// This would yield the same result
// "select * from Table1 where f1=@p0 and f2=@p1"
// @p0 = 10, @p1 = 'A'
```

## Named parameters
You can define parameter name in braces instead of index number.
```csharp
var q = new SqlStatement("select * from Table1 where f1={AA} and f2={BB}");
q.PlaceParameter("AA", 10);
q.PlaceParameter("BB", "A");
// "select * from Table1 where f1=@AA and f2=@BB"
// @AA = 10, @BB = 'A'
```

Use IDictionary<string, object> to place multiple named parameters at once.
```csharp
var q = new SqlStatement("select * from Table1 where f1={AA} and f2={BB}");
q.PlaceParameters(new Dictionary<string, object>() { { "AA", 10 }, { "BB", "A" } });
// "select * from Table1 where f1=@AA and f2=@BB"
// @AA = 10, @BB = 'A'
```

Use object's properties for parameter assignment is also possible.
```csharp
var q = new SqlStatement("select * from Table1 where f1={AA} and f2={BB}");
q.PlaceParameters(new { AA = 10, BB = "A" });
// "select * from Table1 where f1=@AA and f2=@BB"
// @AA = 10, @BB = 'A'
```

# Templating
## Nested statement
You can place SqlStatement object itself into braces. It won't be treated as parameter but that SQL will be nested inside.
This will enable you to build a more dynamic query.\
Note that named parameters and statements are globally recognized throughout every connected statements.
```csharp
var q = new SqlStatement("select * from ({SRC}) t1 where {CONDS}");
var qsrc = new SqlStatement("select * from View1");
var qconds = new SqlStatement("f1={AA} and f2={BB}");
q.PlaceStatement("SRC", qsrc);
q.PlaceStatement("CONDS", qconds);
q.PlaceParameters(new { AA = 10, BB = "A" });
// "select * from (select * from View1) t1 where f1=@AA and f2=@BB"
// @AA = 10, @BB = 'A'
```

## List
**SqlPlace.SqlList** class is a special SqlStatement that can aggregate SQLs and join them with specific separator.\
You can also define empty string when there is no items in the list.\
Note how index number of parameters are only refered locally within each listing item.
```csharp
var q = new SqlStatement("select * from Table1 where {CONDS}");
var qconds = new SqlList(" and ", "(1=0)"); // Separator = " and ", Empty string = "(1=0)"
q.PlaceStatement("CONDS", qconds);
// "select * from Table1 where (1=0)"

qconds.Add("(f1={0} or f2={1})", 10, "A");
qconds.Add("f3>{0}", 100);
// "select * from Table1 where (f1=@p0 or f2=@p1) and f3>@p2"
// @p0 = 10, @p1 = 'A', @p2 = 100
```

You can use **AddRange** method to add multiple listing items at once.
```csharp
var q = new SqlStatement("select * from Table1 where {CONDS}");
var qconds = new SqlList(" and ", "(1=0)");
q.PlaceStatement("CONDS", qconds);
var items = new[] { 
    new SqlStatement("(f1={0} or f2={1})", 10, "A"), 
    new SqlStatement("f3>{0}", 100) 
};
qconds.AddRange(items);
// "select * from Table1 where (f1=@p0 or f2=@p1) and f3>@p2"
// @p0 = 10, @p1 = 'A', @p2 = 100
```

## Predefined lists
SqlList has several predefined creation methods. They all are basically just SqlLists with different construction parameters.
You don't really have to use them. But if you do, they might help improve readability to your code.

**AndClauses** to compose "where" conditions.
```csharp
var q = new SqlStatement("select * from Table1 where {CONDS}");
var qconds = q.PlaceStatement("CONDS", SqlList.AndClauses()) as SqlList;
qconds.Add("(f1={0} or f2={1})", 10, "A");
qconds.Add("f3>{0}", 100);
// "select * from Table1 where (f1=@p0 or f2=@p1) and f3>@p2"
// @p0 = 10, @p1 = 'A', @p2 = 100
```

**CommaValues** to compose "in" conditions.
```csharp
var q = new SqlStatement("select * from Table1 where f1 in ({VALS})");
var vals = new object[] { 10, 20, 30 };
q.PlaceStatement("VALS", SqlList.CommaValues(vals));
// "select * from Table1 where f1 in (@p0, @p1, @p2)"
// @p0 = 10, @p1 = 20, @p2 = 30
```

**CommaClauses** to compose "select" or "insert" fields.
```csharp
var q = new SqlStatement("select {FLDS} from Table1");
var qflds = q.PlaceStatement("FLDS", SqlList.CommaClauses()) as SqlList;
qflds.Add("f1");
qflds.Add("f2");
qflds.Add("{0} f3", 100);
// "select f1, f2, @p0 f3 from Table1"
// @p0 = 100
```
```csharp
var q = new SqlStatement("insert into Table1({FLDS}) values({VALS})");
var flds = new string[] {"f1", "f2"};
var vals = new object[] {10, "A"};
q.PlaceStatement("FLDS", SqlList.CommaClauses(flds));
q.PlaceStatement("VALS", SqlList.CommaValues(vals));
// "insert into Table1(f1, f2) values(@p0, @p1)"
// @p0 = 10, @p1 = 'A'
```

**CommaAssignments** to compose "update" assignments.
```csharp
var q = new SqlStatement("update Table1 set {ASGNS} where f1=0");
var pairs = new Dictionary<string, object>() { { "f1", 10 }, { "f2", "A" } };
q.PlaceStatement("ASGNS", SqlList.CommaAssignments(pairs));
// "update Table1 set f1=@p0, f2=@p1 where f1=0"
// @p0 = 10, @p1 = 'A'
```

# Helper extensions
There are extension methods in namespace **SqlPlace.Extensions** to help you work better with SqlStatement class.

## Execution
Codes below show examples of extension methods those let DbConnection executes SqlPlace.SqlStatement and get the result directly without having to construct DbCommand first.
```csharp
// DbCommand's execution wrappers

int affects = conn.ExecuteNonQuery(q);

object val = conn.ExecuteScalar(q);

IDataReader reader = conn.ExecuteReader(q);
```
```csharp
// Execution methods to work with DataTable

conn.ExecuteFill(dt, q); // where dt is a DataTable

DataTable dt = conn.ExecuteToDataTable(q);

MyDataTable dt = conn.ExecuteToDataTable<MyDataTable>(q);
```
```csharp
// Execution methods to work with value object, POCO, or dictionary

IEnumerable<int?> values = conn.ExecuteToValues<int?>(q);

IEnumerable<MyClass> myclasses = conn.ExecuteToObjects<MyClass>(q);

IEnumerable<IDictionary<string, object>> dicts = conn.ExecuteToDictionaries(q);
```

To use DbTransaction.
```csharp
conn.ExecuteNonQuery(q, trans);
```

To change DbCommand execution timeout.
```csharp
q.Timeout = 60;
```

## Extract object's properties
This extension method can extract properties from various type of object into IDictionary<string, object>.
```csharp
var myclassObject = new MyClass() { f1 = 10, f2 = "A", f3 = 100 };
var anonymousObject = new { f1 = 10, f2 = "A", f3 = 100 };
dynamic dynamicObject = new ExpandoObject();
dynamicObject.f1 = 10;
dynamicObject.f2 = "A";
dynamicObject.f3 = 100;

// All lines below would return the same result i.e. Dictionary { { "f1", 10 }, { "f2", "A" } }
var dict1 = myclassObject.ExtractProperties("f1", "f2");
var dict2 = anonymousObject.ExtractProperties("f1", "f2");
var dict3 = ExtensionMethods.ExtractProperties(dynamicObject, "f1", "f2");
```

# Advance
## Parameter info
To specify database type for a parameter.
```csharp
var dateValue = new DateTime(1970, 1, 1);
q = new SqlStatement("{0}, {1}, {2}", 
    dateValue, 
    new ParameterInfo(dateValue, DbType.DateTime2), 
    new ParameterInfo(dateValue) { SpecificDbType = (int)SqlDbType.SmallDateTime });
// @p0 = '1970-01-01' (datetime)
// @p1 = '1970-01-01' (datetime2)
// @p2 = '1970-01-01' (smalldatetime)
```

To run stored procedure with return/output values
```csharp
var q = new SqlStatement("StoredProcedure1");
q.CommandType = CommandType.StoredProcedure;
// Since there is no reference to parameters in CommandText
// So the order of placing does matter for some DB provider
q.PlaceParameter("preturn", new ParameterInfo() { DbType = DbType.Int32, Direction = ParameterDirection.ReturnValue });
q.PlaceParameter("pinput", 123);
q.PlaceParameter("poutput", new ParameterInfo() { DbType = DbType.AnsiString, Size = 50, Direction = ParameterDirection.Output });
conn.ExecuteNonQuery(q, trans);
var returnValue = q.ParameterValue("preturn");
var inputValue = q.ParameterValue("pinput");
var outputValue = q.ParameterValue("poutput");
```

## Other DB Providers
SqlPlace comes with CommandFactory for SqlClient, OleDb and Odbc provider out of the box.
Typically, you don't have to set SqlStatement's CommandFactory yourself
as there is a generic class that will try to resolve DbConnection 
and infer parameter's behavior of that DB provider automatically.
Even if the provider is not the three above.

However, there is some case that this generic class might failed to infer DB provider's behavior correctly.
A concrete implementation of such DB provider would be required.
Code below shows an example of how to implement CommandFactory for Oracle's ODP.NET.
```csharp
public class OracleCommandFactory : SqlPlace.Factories.GenericCommandFactory
{
    static int _ = Register<OracleConnection, OracleCommandFactory>();

    public OracleCommandFactory() : base(OracleClientFactory.Instance)
    {

    }

    public override DbCommand CreateCommand()
    {
        var cmd = base.CreateCommand() as OracleCommand;
        cmd.BindByName = true;
        return cmd;
    }

    public override string SpecificDbTypePropertyName()
    {
        return "OracleDbType";
    }

    public override bool IsSupportNamedParameter()
    {
        return true;
    }
}
```

# SQL Dialect (Experimental feature)
## String implicit conversion
Before we dive into SQL dialect feature. 
It is worth noting how SqlStatement works on string implicit conversion.
Take a look at this csharp code.
```csharp
var q = new SqlStatement("select f1, getdate() f2 from Table1 where f1>{A}");
// Will Make into "select f1, getdate() f2 from Table1 where f1>@A"
```
With the new string implicit conversion feature. You can also write the code like this.
```csharp
SqlStatement q = "select f1, getdate() f2 from Table1 where f1>{A}";
// Will Make into "select f1, getdate() f2 from Table1 where f1>@A" too
```
## SqlDialect
We are going to change the "getdate()" in the query above with something dialect dependent.\
There is **SqlPlace.SqlDialect** class to handle all this things.\
First you will have to set **SqlDialect.DefaultDialectName** or it will, by default, depend on DefaultCommandFactory.\
Then we will replace "getdate()" with dialect dependent function for "current date" like this.
```csharp
SqlStatement q = "select f1, " + SqlDialect.CurrentDate() + " f2 from Table1 where f1>{A}";
// If DefaultDialectName is "MSSQL"
// Then this code will Make into 
// "select f1, CONVERT(DATE, GETDATE()) f2 from Table1 where f1>@A"
```
You can make the code shorter by define an alias for SqlDialect at the beginning of your source code.
```csharp
using sd = SqlPlace.SqlDialect;
...
SqlStatement q = "select f1, " + sd.CurrentDate() + " f2 from Table1 where f1>{A}";
```
You can make the code even shorter using string interpolation.
```csharp
SqlStatement q = $"select f1, {sd.CurrentDate()} f2 from Table1 where f1>{{A}}";
// Note the parameter "A" must be escaped here
```
If we change DefaultDialectName to something SqlDialect cannot resolve. It will return SQL standard one for "current date".
```csharp
sd.DefaultDialectName = "Unknown";
SqlStatement q = $"select f1, {sd.CurrentDate()} f2 from Table1 where f1>{{A}}";
// Will Make into
// "select f1, CURRENT_DATE f2 from Table1 where f1>@A"
```
And yes. All dialectal statements can be nested all the way. So the query construction like this is compleletely valid.
```csharp
SqlStatement q = sd.Select($"f1, {sd.CurrentDate()} f2",
                    From: "(" + sd.Select("*", 
                                    From: "Table1", 
                                    Where: $"{sd.IsNull("x", "0")}>{{B}}") + ") t1",
                    Where: "f1>{A}",
                    Offset: 100, Fetch: 50);
```
