# SqlPlace

[![SqlPlace on Nuget](https://img.shields.io/nuget/vpre/SqlPlace.svg)](https://www.nuget.org/packages/SqlPlace)

SqlPlace is a .NET framework library to help you build complex parameterized SQL query.

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
- [ETC.](#etc)    

# Basic usage
Use **SqlPlace.SqlStatement** class to compose SQL and construct ADO.NET DbCommand.
```csharp
using(var conn = new SqlConnection(connString))
{
    var q = new SqlPlace.SqlStatement("select * from Table1");

    // SqlCommand is the default one. Change only if you want to use other provider.
    q.CommandFactory = new SqlPlace.Factories.SqlCommandFactory();
    var cmd = q.ToCommand(conn);

    // cmd is SqlCommand. You can do any ADO.NET execution with it.

}
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

# Templating
## Nested statement
You can place SqlStatement object itself into braces. It won't be interpreted as parameter but that SQL will be nested inside.
This will enable you to build a more dynamic query.\
Note that named parameters are globally recognized throughout every connected statements.
```csharp
var q = new SqlStatement("select * from ({SRC}) t1 where {CONDS}");
var qsrc = new SqlStatement("select * from View1");
var qconds = new SqlStatement("f1={AA} and f2={BB}");
q.PlaceStatement("SRC", qsrc);
q.PlaceStatement("CONDS", qconds);
q.PlaceParameters(new Dictionary<string, object>() { { "AA", 10 }, { "BB", "A" } });
// "select * from (select * from View1) t1 where f1=@AA and f2=@BB"
// @AA = 10, @BB = 'A'
```

## List
**SqlPlace.SqlList** class is a special SqlStatement that can aggregate SQLs and join them with specific separator.\
You can also define empty string when there is no items in the list.\
Note how index number of parameters are only refered locally within each listing item.
```csharp
var q = new SqlStatement("select * from Table1 where {CONDS}");
var qconds = new SqlList(" and ", "(1=0)");
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
var items = new[] { new SqlStatement("(f1={0} or f2={1})", 10, "A"), new SqlStatement("f3>{0}", 100) };
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

var affects = conn.ExecuteNonQuery(q);

var val = conn.ExecuteScalar(q);

var reader = conn.ExecuteReader(q);
```
```csharp
// Execution methods to work with DataTable

conn.ExecuteFill(dt, q); // where dt is a DataTable

var dt = conn.ExecuteToDataTable(q);

var dt = conn.ExecuteToDataTable<MyDataTable>(q);
```
```csharp
// Execution methods to work with value object, POCO, or dictionary

var values = conn.ExecuteToValues<int?>(q);

var myclasses = conn.ExecuteToObjects<MyClass>(q);

var dicts = conn.ExecuteToDictionaries(q);
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

# ETC.

To specify database type for a parameter.
```csharp
var q = new SqlStatement("select * from Table1 where f1={0} and f2={1}", 10, new ParameterInfo("A", SqlDbType.VarChar));
// "select * from Table1 where f1=@p0 and f2=@p1"
// @p0 = 10, @p1 = 'A' (ANSI string)
```

To run store procedure with output values
```csharp
var q = new SqlStatement("StoreProcedure1");
q.CommandType = CommandType.StoredProcedure;
q.PlaceParameter("pinput", 123);
q.PlaceParameter("poutput", new ParameterInfo() { SqlDbType = SqlDbType.VarChar, Size = 50, Direction = ParameterDirection.Output });
q.PlaceParameter("preturn", new ParameterInfo() { SqlDbType = SqlDbType.Int, Direction = ParameterDirection.ReturnValue });
conn.ExecuteNonQuery(q, trans);
var inputValue = q.ParameterValue("pinput");
var outputValue = q.ParameterValue("poutput");
var returnValue = q.ParameterValue("preturn");
```

To change DbCommand timeout.
```csharp
q.Timeout = 60;
```

To use DbTransaction
```csharp
conn.ExecuteNonQuery(q, trans);
```

To get bare CommandText and Parameters' information instead of constructing DbCommand.
```csharp
var q = new SqlStatement("select * from Table1 where f1={0}", 10);
CommandInfo cmdInfo = q.Make();
string commandText = cmdInfo.CommandText; // "select * from Table1 where f1=@p0"
string paramName0 = cmdInfo.Parameters[0].ParameterName; // "@p0"
object paramValue0 = cmdInfo.Parameters[0].Value; // 10
```

To get SQL as unparameterized plain text.
```csharp
q = new SqlStatement("select * from Table1 where f1={0} and f2={1} and f3={2}", 10, "A", new DateTime(1970, 1, 1));
var sql = q.PlainText(); // "select * from Table1 where f1=10 and f2='A' and f3='1970-01-01 00:00:00'"
```
