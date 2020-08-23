using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using SqlPlace;
using SqlPlace.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject
{
    [TestClass]
    public class UnitTest1
    {
        SqlStatement q;
        DbCommand cmd;

        string xName = "John";
        DateTime xCreateDate = new DateTime(2010, 1, 3);
        int xStatus = 3;
        DateTime? xUpdateDate = null;

        [TestMethod]
        public void TestConstructor()
        {
            // Basic Usage
            q = new SqlStatement("select * from User where name={3} and create_date={2} and status={1} and update_date={0}",
                xUpdateDate, xStatus, xCreateDate, xName);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@p3 and create_date=@p2 and status=@p1 and update_date=@p0", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(SqlDbType.NVarChar, (cmd.Parameters["@p3"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p0"].Value);

            // Detailed parameters
            q = new SqlStatement("select * from User where name={3} and create_date={2} and status={1} and update_date={0}",
                xUpdateDate, xStatus, xCreateDate, new ParameterInfo(xName, DbType.AnsiString));
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@p3 and create_date=@p2 and status=@p1 and update_date=@p0", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@p3"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p0"].Value);

            // Named (global) parameters
            q = new SqlStatement("select * from User where name={N} and create_date={C} and status={S} and update_date={U}",
                new Dictionary<string, object> { { "U", xUpdateDate }, { "S", xStatus }, { "C", xCreateDate }, { "N", new ParameterInfo(xName, DbType.AnsiString) } });
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@N and create_date=@C and status=@S and update_date=@U", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@N"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@N"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@C"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@S"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@U"].Value);

            // Named parameter from object's properties
            q = new SqlStatement("select * from User where name={N} and create_date={C} and status={S} and update_date={U}",
                new { U = xUpdateDate, S = xStatus, C = xCreateDate, N = new ParameterInfo(xName, DbType.AnsiString) });
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@N and create_date=@C and status=@S and update_date=@U", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@N"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@N"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@C"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@S"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@U"].Value);
        }

        [TestMethod]
        public void TestPlaceParameter()
        {
            // Placing local parameters individually
            q = new SqlStatement("select * from User where name={3} and create_date={2} and status={1} and update_date={0}");
            q.PlaceParameter(0, xUpdateDate);
            q.PlaceParameter(1, xStatus);
            q.PlaceParameter(2, xCreateDate);
            q.PlaceParameter(3, new ParameterInfo(xName, DbType.AnsiString));
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@p3 and create_date=@p2 and status=@p1 and update_date=@p0", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@p3"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p0"].Value);

            // Placing local and global parameters
            q = new SqlStatement("select * from User where name={N} and create_date={C} and status={0} and update_date={1}", xStatus, xUpdateDate);
            q.PlaceParameter("C", xCreateDate);
            q.PlaceParameter("N", new ParameterInfo(xName, DbType.AnsiString));
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@N and create_date=@C and status=@p0 and update_date=@p1", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@N"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@N"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@C"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p1"].Value);
        }

        [TestMethod]
        public void TestPlaceParameters()
        {
            // Dictionry
            q = new SqlStatement("select * from T1 where f1={A} and f2={B}");
            q.PlaceParameters(new Dictionary<string, object>() { { "A", 1 }, { "B", 2 } });
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where f1=@A and f2=@B", cmd.CommandText);
            Assert.AreEqual(1, cmd.Parameters["@A"].Value);
            Assert.AreEqual(2, cmd.Parameters["@B"].Value);

            // Paremeter object
            q = new SqlStatement("select * from T1 where f1={A} and f2={B}");
            q.PlaceParameters(new { A = 1, B = 2 });
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where f1=@A and f2=@B", cmd.CommandText);
            Assert.AreEqual(1, cmd.Parameters["@A"].Value);
            Assert.AreEqual(2, cmd.Parameters["@B"].Value);

            // One value parameter should not be confused with one parameter object
            q = new SqlStatement("select * from T1 where f1={0}");
            q.PlaceParameters(1);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where f1=@p0", cmd.CommandText);
            Assert.AreEqual(1, cmd.Parameters["@p0"].Value);
        }

        [TestMethod]
        public void TestPlaceStatement()
        {
            // Placing nested query
            q = new SqlStatement("select * from User where name={N} and create_date={C} and {MORE}");
            q.PlaceStatement("MORE", "status={0} and update_date={1}", xStatus, xUpdateDate);
            q.PlaceParameter("C", xCreateDate);
            q.PlaceParameter("N", xName);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@N and create_date=@C and status=@p0 and update_date=@p1", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@N"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@C"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p1"].Value);

            // Mixed local and global parameter with subquery
            q = new SqlStatement("select * from User where name={0} and {MORE} and update_date={U}");
            //q.PlaceSql("MORE", "create_date={0} and status={S}", xCreateDate);
            q.PlaceStatement("MORE", new SqlStatement("create_date={0} and status={S}", xCreateDate));
            q.PlaceParameter("U", xUpdateDate);
            q.PlaceParameter("S", xStatus);
            q.PlaceParameter(0, xName);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@p0 and create_date=@p1 and status=@S and update_date=@U", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@S"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@U"].Value);

            // Global parameter can be placed in subquery
            q = new SqlStatement("select * from User where name={0} and {MORE} and update_date={U}");
            q.PlaceParameter(0, xName);
            q.PlaceParameter("U", xUpdateDate);
            q.PlaceStatement("MORE", new SqlStatement("create_date={0} and status={S}", xCreateDate)).PlaceParameter("S", xStatus);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@p0 and create_date=@p1 and status=@S and update_date=@U", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@S"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@U"].Value);
        }

        [TestMethod]
        public void TestParameterOrder()
        {
            // Preorder Traversal Statement placing
            q = new SqlStatement("{A}-{1}-{0}-{MORE1}-{B}-{MORE2}", 10, 11);
            q.PlaceStatement("MORE1", "({0}+{A}+{B}+{1}+{MORE3})", 21, 22);
            q.PlaceStatement("MORE2", "({MORE3}*{0}*{C})", 31);
            q.PlaceStatement("MORE3", "({D}/{0})", 41);
            q.PlaceParameter("A", "a");
            q.PlaceParameter("B", "b");
            q.PlaceParameter("C", "c");
            q.PlaceParameter("D", "d");
            cmd = q.MakeCommand();
            Assert.AreEqual("@A-@p1-@p0-(@p2+@A+@B+@p3+(@D/@p4))-@B-((@D/@p6)*@p5*@C)", cmd.CommandText);
            Assert.AreEqual("a", cmd.Parameters["@A"].Value);
            Assert.AreEqual("b", cmd.Parameters["@B"].Value);
            Assert.AreEqual("c", cmd.Parameters["@C"].Value);
            Assert.AreEqual("d", cmd.Parameters["@D"].Value);
            Assert.AreEqual(10, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(11, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(21, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(22, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(41, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(31, cmd.Parameters["@p5"].Value);
            Assert.AreEqual(41, cmd.Parameters["@p6"].Value);
            Assert.AreEqual("'a'-11-10-(21+'a'+'b'+22+('d'/41))-'b'-(('d'/41)*31*'c')", q.MakeText());

            // OLE DB does not support named parameters
            q.CommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
            cmd = q.MakeCommand();
            //Assert.AreEqual("@p0-@p1-@p2-(@p3+@p4+@p5+@p6+(@p7/@p8))-@p9-((@p10/@p11)*@p12*@p13)", cmd.CommandText);
            Assert.AreEqual("?-?-?-(?+?+?+?+(?/?))-?-((?/?)*?*?)", cmd.CommandText);
            Assert.AreEqual("a", cmd.Parameters["p0"].Value);
            Assert.AreEqual(11, cmd.Parameters["p1"].Value);
            Assert.AreEqual(10, cmd.Parameters["p2"].Value);
            Assert.AreEqual(21, cmd.Parameters["p3"].Value);
            Assert.AreEqual("a", cmd.Parameters["p4"].Value);
            Assert.AreEqual("b", cmd.Parameters["p5"].Value);
            Assert.AreEqual(22, cmd.Parameters["p6"].Value);
            Assert.AreEqual("d", cmd.Parameters["p7"].Value);
            Assert.AreEqual(41, cmd.Parameters["p8"].Value);
            Assert.AreEqual("b", cmd.Parameters["p9"].Value);
            Assert.AreEqual("d", cmd.Parameters["p10"].Value);
            Assert.AreEqual(41, cmd.Parameters["p11"].Value);
            Assert.AreEqual(31, cmd.Parameters["p12"].Value);
            Assert.AreEqual("c", cmd.Parameters["p13"].Value);
            Assert.AreEqual("'a'-11-10-(21+'a'+'b'+22+('d'/41))-'b'-(('d'/41)*31*'c')", q.MakeText());
        }

        object[] set1 = { new ParameterInfo("1", DbType.AnsiString), "3", "5" };
        object[] set2 = { new ParameterInfo(1, DbType.Byte), 3, 5 };

        [TestMethod]
        public void TestList()
        {
            // Place list
            q = new SqlStatement("select * from User where {CONDS}");
            var cond = q.PlaceStatement("CONDS", new SqlList(" and ", "(1=1)")) as SqlList;
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where (1=1)", cmd.CommandText);
            cond.Add("name={0}", xName);
            cond.Add("create_date={0}", xCreateDate);
            cond.Add("status={0}", xStatus);
            cond.Add("update_date={0}", xUpdateDate);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from User where name=@p0 and create_date=@p1 and status=@p2 and update_date=@p3", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p3"].Value);

            // Place And/Comma Collection
            q = new SqlStatement("select {FLDS} from User where {CONDS}");
            cond = q.PlaceStatement("CONDS", SqlList.AndClauses()) as SqlList;
            cond.Add("name={0}", xName);
            cond.Add("create_date={0}", xCreateDate);
            cond.Add("status={0}", xStatus);
            cond.Add("update_date={0}", xUpdateDate);
            var flds = q.PlaceStatement("FLDS", SqlList.CommaClauses()) as SqlList;
            flds.Add("f1");
            flds.Add("f2");
            flds.Add("f3");
            cmd = q.MakeCommand();
            Assert.AreEqual("select f1, f2, f3 from User where name=@p0 and create_date=@p1 and status=@p2 and update_date=@p3", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p3"].Value);

            // Local & global token in collection
            var fields = new Dictionary<string, object>() { { "name={0}", "AAA" }, { "gender={0}", 1 } };
            var conditions = new Dictionary<string, object>() { { "section={0}", 2 }, { "status={0}", 3 }, { "region=@R", 99 }, { "country='TH'", null } };
            q = new SqlStatement("update Table1 set {FLDS} where {CONDS}");
            (q.PlaceStatement("FLDS", SqlList.CommaClauses()) as SqlList).AddRange(fields);
            (q.PlaceStatement("CONDS", SqlList.AndClauses()) as SqlList).AddRange(conditions);
            q.PlaceParameter("R", 98);
            cmd = q.MakeCommand();
            Assert.AreEqual("update Table1 set name=@p0, gender=@p1 where section=@p2 and status=@p3 and region=@R and country='TH'", cmd.CommandText);
            Assert.AreEqual("AAA", cmd.Parameters["@p0"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(2, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(98, cmd.Parameters["@R"].Value);

            var assigns = new Dictionary<string, object>() { { "name", "AAA" }, { "gender", 1 } };
            q = new SqlStatement("update Table1 set {FLDS} where {CONDS}");
            q.PlaceStatement("FLDS", SqlList.CommaAssignments(assigns));
            (q.PlaceStatement("CONDS", SqlList.AndClauses()) as SqlList).AddRange(conditions);
            q.PlaceParameter("R", 98);
            cmd = q.MakeCommand();
            Assert.AreEqual("update Table1 set name=@p0, gender=@p1 where section=@p2 and status=@p3 and region=@R and country='TH'", cmd.CommandText);
            Assert.AreEqual("AAA", cmd.Parameters["@p0"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(2, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(98, cmd.Parameters["@R"].Value);

            // Comma Values
            q = new SqlStatement("select * from T1 where F1 in ({SET1}) and F2 in ({SET2})");
            q.PlaceStatement("SET1", SqlList.CommaValues(set1));
            q.PlaceStatement("SET2", SqlList.CommaValues(set2));
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where F1 in (@p0, @p1, @p2) and F2 in (@p3, @p4, @p5)", cmd.CommandText);
            Assert.AreEqual("1", cmd.Parameters["@p0"].Value);
            Assert.AreEqual("3", cmd.Parameters["@p1"].Value);
            Assert.AreEqual("5", cmd.Parameters["@p2"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(5, cmd.Parameters["@p5"].Value);
        }

        [TestMethod]
        public void TestPlaceStatementInParameter()
        {
            // Place subquery instead of parameter
            q = new SqlStatement("select * from T1 where F1 in ({0}) and F2 in ({X})", SqlList.CommaValues(set1));
            q.PlaceParameter("X", SqlList.CommaValues(set2));
            cmd = q.MakeCommand();
            cmd = q.MakeCommand(); // Do it again should yield the same SQL
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where F1 in (@p0, @p1, @p2) and F2 in (@p3, @p4, @p5)", cmd.CommandText);
            Assert.AreEqual("1", cmd.Parameters["@p0"].Value);
            Assert.AreEqual("3", cmd.Parameters["@p1"].Value);
            Assert.AreEqual("5", cmd.Parameters["@p2"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(5, cmd.Parameters["@p5"].Value);

            // the use of list as PlaceStatement named parameters
            q = new SqlStatement("select * from T1 where {FILTER}");
            q.PlaceStatement("FILTER", "F1 in ({A}) and F2 in ({B})", new Dictionary<string, object>() { { "A", SqlList.CommaValues(set1) }, { "B", SqlList.CommaValues(set2) } });
            cmd = q.MakeCommand();
            cmd = q.MakeCommand(); // Do it again should yield the same SQL
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where F1 in (@p0, @p1, @p2) and F2 in (@p3, @p4, @p5)", cmd.CommandText);
            Assert.AreEqual("1", cmd.Parameters["@p0"].Value);
            Assert.AreEqual("3", cmd.Parameters["@p1"].Value);
            Assert.AreEqual("5", cmd.Parameters["@p2"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(5, cmd.Parameters["@p5"].Value);

            //  the use of list as PlaceStatement indexed parameters
            q = new SqlStatement("select * from T1 where {FILTER}");
            q.PlaceStatement("FILTER", "F1 in ({0}) and F2 in ({1})", SqlList.CommaValues(set1), SqlList.CommaValues(set2));
            cmd = q.MakeCommand();
            cmd = q.MakeCommand(); // Do it again should yield the same SQL
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where F1 in (@p0, @p1, @p2) and F2 in (@p3, @p4, @p5)", cmd.CommandText);
            Assert.AreEqual("1", cmd.Parameters["@p0"].Value);
            Assert.AreEqual("3", cmd.Parameters["@p1"].Value);
            Assert.AreEqual("5", cmd.Parameters["@p2"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(5, cmd.Parameters["@p5"].Value);
        }

        [TestMethod]
        public void TestMakeText()
        {
            q = new SqlStatement("select * from T1 where {FILTER}");
            q.PlaceStatement("FILTER", "F1 in ({0}) and F2 in ({1})", SqlList.CommaValues(set1), SqlList.CommaValues(set2));

            q.CommandFactory = new SqlPlace.Factories.SqlCommandFactory();
            Assert.AreEqual("select * from T1 where F1 in ('1', '3', '5') and F2 in (1, 3, 5)", q.MakeText());

            q.CommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
            Assert.AreEqual("select * from T1 where F1 in ('1', '3', '5') and F2 in (1, 3, 5)", q.MakeText());

            q.CommandFactory = new SqlPlace.Factories.OdbcCommandFactory();
            Assert.AreEqual("select * from T1 where F1 in ('1', '3', '5') and F2 in (1, 3, 5)", q.MakeText());


            q = new SqlStatement("{B} {0} {A} {2} {1}");
            q.PlaceParameter(0, 10);
            q.PlaceParameter(1, 20);
            q.PlaceParameter(2, 30);
            q.PlaceParameter("A", 100);
            q.PlaceParameter("B", 200);

            q.CommandFactory = new SqlPlace.Factories.SqlCommandFactory();
            Assert.AreEqual("200 10 100 30 20", q.MakeText());

            q.CommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
            Assert.AreEqual("200 10 100 30 20", q.MakeText());

            q.CommandFactory = new SqlPlace.Factories.OdbcCommandFactory();
            Assert.AreEqual("200 10 100 30 20", q.MakeText());

        }

        [TestMethod]
        public void TestMake()
        {
            // ==== Has Placeholders in CommandText
            q = new SqlStatement("{B} {0} {A} {2} {1}");
            q.PlaceParameter(0, 10);
            q.PlaceParameter(1, 20);
            q.PlaceParameter(2, 30);
            q.PlaceParameter("A", 100);
            q.PlaceParameter("B", 200);

            // CommandType Text
            q.CommandFactory = new SqlPlace.Factories.SqlCommandFactory();
            var cmdInfo = q.Make();
            Assert.AreEqual("@B @p0 @A @p2 @p1", cmdInfo.CommandText);
            Assert.AreEqual("@p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("@p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("@p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("@A", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("@B", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[4].Value);

            q.CommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
            cmdInfo = q.Make();
            Assert.AreEqual("? ? ? ? ?", cmdInfo.CommandText);
            Assert.AreEqual("B", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p0", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[4].Value);

            q.CommandFactory = new SqlPlace.Factories.OdbcCommandFactory();
            cmdInfo = q.Make();
            Assert.AreEqual("? ? ? ? ?", cmdInfo.CommandText);
            Assert.AreEqual("B", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p0", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[4].Value);

            // CommandType StoredProcedure
            q.CommandType = CommandType.StoredProcedure; ;

            q.CommandFactory = new SqlPlace.Factories.SqlCommandFactory();
            cmdInfo = q.Make();
            Assert.AreEqual("@B @p0 @A @p2 @p1", cmdInfo.CommandText);
            Assert.AreEqual("@p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("@p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("@p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("@A", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("@B", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[4].Value);

            q.CommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
            cmdInfo = q.Make();
            Assert.AreEqual("? ? ? ? ?", cmdInfo.CommandText);
            Assert.AreEqual("B", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p0", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[4].Value);

            q.CommandFactory = new SqlPlace.Factories.OdbcCommandFactory();
            cmdInfo = q.Make();
            Assert.AreEqual("? ? ? ? ?", cmdInfo.CommandText);
            Assert.AreEqual("B", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p0", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[4].Value);


            // ==== Has No Placeholders in CommandText
            q = new SqlStatement("CommandText");
            q.PlaceParameter("B", 200);
            q.PlaceParameter(0, 10);
            q.PlaceParameter("A", 100);
            q.PlaceParameter(2, 30);
            q.PlaceParameter(1, 20);

            // CommandType Text
            q.CommandFactory = new SqlPlace.Factories.SqlCommandFactory();
            cmdInfo = q.Make();
            Assert.AreEqual("CommandText", cmdInfo.CommandText);
            Assert.AreEqual("@p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("@p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("@p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("@B", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("@A", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[4].Value);

            q.CommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
            cmdInfo = q.Make();
            // Actually this use case shouldn't happen (CommandType Text without parameter placeholders)
            Assert.AreEqual("CommandText", cmdInfo.CommandText);
            Assert.AreEqual("p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("B", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[4].Value);

            q.CommandFactory = new SqlPlace.Factories.OdbcCommandFactory();
            cmdInfo = q.Make();
            // Actually this use case shouldn't happen (CommandType Text without parameter placeholders)
            Assert.AreEqual("CommandText", cmdInfo.CommandText);
            Assert.AreEqual("p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("B", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[4].Value);

            // CommandType StoredProcedure
            q.CommandType = CommandType.StoredProcedure; ;

            q.CommandFactory = new SqlPlace.Factories.SqlCommandFactory();
            cmdInfo = q.Make();
            Assert.AreEqual("CommandText", cmdInfo.CommandText);
            Assert.AreEqual("@p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("@p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("@p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("@B", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("@A", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[4].Value);
            
            q.CommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
            cmdInfo = q.Make();
            // Actually this use case shouldn't happen (CommandType StoredProcedure with indexed parameters)
            Assert.AreEqual("CommandText", cmdInfo.CommandText);
            Assert.AreEqual("p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("B", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[4].Value);

            q.CommandFactory = new SqlPlace.Factories.OdbcCommandFactory();
            // Actually this use case shouldn't happen (CommandType StoredProcedure with indexed parameters)
            cmdInfo = q.Make();
            Assert.AreEqual("CommandText", cmdInfo.CommandText);
            Assert.AreEqual("p0", cmdInfo.Parameters[0].ParameterName); Assert.AreEqual(10, cmdInfo.Parameters[0].Value);
            Assert.AreEqual("p1", cmdInfo.Parameters[1].ParameterName); Assert.AreEqual(20, cmdInfo.Parameters[1].Value);
            Assert.AreEqual("p2", cmdInfo.Parameters[2].ParameterName); Assert.AreEqual(30, cmdInfo.Parameters[2].Value);
            Assert.AreEqual("B", cmdInfo.Parameters[3].ParameterName); Assert.AreEqual(200, cmdInfo.Parameters[3].Value);
            Assert.AreEqual("A", cmdInfo.Parameters[4].ParameterName); Assert.AreEqual(100, cmdInfo.Parameters[4].Value);

        }



        [TestMethod]
        public void TestStoreProcedureCommand()
        {
            // Store Procedure = Command without {tag} but has named parameter
            q = new SqlStatement("sp_StoreProc") { CommandType = CommandType.StoredProcedure };
            q.PlaceParameter("A", 1);
            q.PlaceParameter("B", new ParameterInfo(10, DbType.Int32) { Direction = ParameterDirection.Output });
            cmd = q.MakeCommand();
            Assert.AreEqual("sp_StoreProc", cmd.CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual(1, cmd.Parameters["@A"].Value);
            Assert.AreEqual(ParameterDirection.Input, cmd.Parameters["@A"].Direction);
            Assert.AreEqual(10, cmd.Parameters["@B"].Value);
            Assert.AreEqual(ParameterDirection.Output, cmd.Parameters["@B"].Direction);
        }

        [TestMethod]
        public void TestNullHandling()
        {
            q = new SqlStatement("select * from T1 where f1={0} and f2={1}", null, DBNull.Value);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where f1=@p0 and f2=@p1", cmd.CommandText);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(DBNull.Value, cmd.Parameters["@p1"].Value);
            Assert.AreEqual("select * from T1 where f1=null and f2=null", q.MakeText());
        }

        [TestMethod]
        public void TestPlaceArrayInParameter()
        {
            // Auto-boxing array to list
            var whereIn = new object[] { 20, 30, 40 };
            q = new SqlStatement("select * from T1 where f1={0} and f2 in ({1})", 10, whereIn);
            cmd = q.MakeCommand();
            Assert.AreEqual("select * from T1 where f1=@p0 and f2 in (@p1, @p2, @p3)", cmd.CommandText);
            Assert.AreEqual(10, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(20, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(30, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(40, cmd.Parameters["@p3"].Value);

        }

        [TestMethod]
        public void TestSpecificDbType()
        {
            var dateValue = new DateTime(1970, 1, 1);
            q = new SqlStatement("{0}, {1}, {2}", 
                dateValue, 
                new ParameterInfo(dateValue, DbType.DateTime2), 
                new ParameterInfo(dateValue) { SpecificDbType = (int)SqlDbType.SmallDateTime });
            cmd = q.MakeCommand();
            Assert.AreEqual(dateValue, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(dateValue, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(dateValue, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(SqlDbType.DateTime, (cmd.Parameters["@p0"] as SqlParameter).SqlDbType);
            Assert.AreEqual(SqlDbType.DateTime2, (cmd.Parameters["@p1"] as SqlParameter).SqlDbType);
            Assert.AreEqual(SqlDbType.SmallDateTime, (cmd.Parameters["@p2"] as SqlParameter).SqlDbType);
        }

        [TestMethod]
        public void TestEscape()
        {
            q = new SqlStatement(@"select '{ ""A"" : 10 }' json from Table1");
            cmd = q.MakeCommand();
            Assert.AreEqual(@"select '{ ""A"" : 10 }' json from Table1", cmd.CommandText);

            q = new SqlStatement(@"select '{{A}}' json from Table1");
            cmd = q.MakeCommand();
            Assert.AreEqual(@"select '{A}' json from Table1", cmd.CommandText);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Unable to place to itself")]
        public void TestSelfPlace()
        {
            var A = new SqlStatement("1+{0}");
            A.PlaceParameter(0, A);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Circular referencing detected")]
        public void TestCircularPlace()
        {
            var A = new SqlStatement("1+{B}");
            var B = new SqlStatement("2+{C}");
            var C = new SqlStatement("3+{A}");
            A.PlaceParameter("B", B);
            B.PlaceParameter("C", C);
            C.PlaceParameter("A", A);
        }

        private class POCO
        {
            public int Field1 { get; set; }
            public string Field2 { get; set; }
            public DateTime? Field3 { get; set; }
        }

        [TestMethod]
        public void TestExecute()
        {
            string connString;
            string providerName;
            try
            {
                connString = System.Configuration.ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString;
                providerName = System.Configuration.ConfigurationManager.ConnectionStrings["ConnString"].ProviderName;
            }
            catch
            {
                Assert.Inconclusive("To run this test; Add app.config with ConnString");
                return;
            }
            using (var conn = DbProviderFactories.GetFactory(providerName).CreateConnection())
            {
                conn.ConnectionString = connString;
                conn.Open();
                var trans = conn.BeginTransaction();

                var q = new SqlStatement("create table #Test(Field1 int, Field2 varchar(50), Field3 datetime)");
                conn.ExecuteNonQuery(q, trans);

                var o1 = new POCO { Field1 = 1, Field2 = "AA", Field3 = new DateTime(2019, 8, 8) };
                var o2 = new POCO { Field1 = 2, Field2 = "BB", Field3 = new DateTime(2020, 8, 8) };
                var o3 = new POCO { Field1 = 3, Field2 = "CC", Field3 = new DateTime(2021, 8, 8) };
                q = new SqlStatement("insert into #Test values({Field1}, {Field2}, {Field3})");
                q.PlaceParameters(o1);
                Assert.AreEqual(1, conn.ExecuteNonQuery(q, trans));
                q.PlaceParameters(o2);
                Assert.AreEqual(1, conn.ExecuteNonQuery(q, trans));
                q.PlaceParameters(o3);
                Assert.AreEqual(1, conn.ExecuteNonQuery(q, trans));

                q = new SqlStatement("select Field2 from #Test where Field1={0}");
                q.PlaceParameter(0, 1);
                Assert.AreEqual("AA", conn.ExecuteScalar(q, trans));
                q.PlaceParameter(0, 3);
                Assert.AreEqual("CC", conn.ExecuteScalar(q, trans));

                q = new SqlStatement("select * from #Test where Field1 > 1 order by Field1");
                using (var rdr = conn.ExecuteReader(q, trans))
                {
                    rdr.Read();
                    Assert.AreEqual("BB", rdr.GetString(1));
                    rdr.Read();
                    Assert.AreEqual("CC", rdr.GetString(1));
                    rdr.Close();
                };

                var dt = new DataTable();
                q = new SqlStatement("select * from #Test");
                conn.ExecuteFill(ref dt, q, trans);
                Assert.AreEqual(3, dt.Rows.Count);

                dt = conn.ExecuteToDataTable(q, trans);
                Assert.AreEqual(3, dt.Rows.Count);

                q = new SqlStatement("select Field3 from #Test");
                var dates = conn.ExecuteToValues<DateTime>(q, trans).ToArray();
                Assert.AreEqual(new DateTime(2019, 8, 8), dates[0]);
                Assert.AreEqual(new DateTime(2020, 8, 8), dates[1]);
                Assert.AreEqual(new DateTime(2021, 8, 8), dates[2]);
                conn.ExecuteNonQuery(new SqlStatement("update #Test set Field3=null where Field1={0}", 2), trans);
                var ndates = conn.ExecuteToValues<DateTime?>(q, trans).ToArray();
                Assert.AreEqual(new DateTime(2019, 8, 8), ndates[0]);
                Assert.IsNull(ndates[1]);
                Assert.AreEqual(new DateTime(2021, 8, 8), ndates[2]);

                q = new SqlStatement("select Field2 from #Test");
                var strings = conn.ExecuteToValues<string>(q, trans).ToArray();
                Assert.AreEqual("AA", strings[0]);
                Assert.AreEqual("BB", strings[1]);
                Assert.AreEqual("CC", strings[2]);
                conn.ExecuteNonQuery(new SqlStatement("update #Test set Field2=null where Field1={0}", 2), trans);
                strings = conn.ExecuteToValues<string>(q, trans).ToArray();
                Assert.AreEqual("AA", strings[0]);
                Assert.IsNull(strings[1]);
                Assert.AreEqual("CC", strings[2]);

                q = new SqlStatement("select * from #Test");
                var pocos = conn.ExecuteToObjects<POCO>(q, trans).ToArray();
                Assert.AreEqual(1, pocos[0].Field1);
                Assert.AreEqual(2, pocos[1].Field1);
                Assert.AreEqual(3, pocos[2].Field1);

                var dyns = conn.ExecuteToDictionaries(q, trans).ToArray();
                Assert.AreEqual(1, dyns[0]["Field1"]);
                Assert.AreEqual(2, dyns[1]["Field1"]);
                Assert.AreEqual(3, dyns[2]["Field1"]);

                q = new SqlStatement(@"CREATE PROCEDURE [StoreProcedure1] @pinput int, @poutput varchar(50) OUTPUT
AS BEGIN select @poutput = '(('+convert(varchar, @pinput)+'))';  return 31; END");
                conn.ExecuteNonQuery(q, trans);

                switch(providerName)
                {
                    case "System.Data.SqlClient": 
                        q = new SqlStatement("StoreProcedure1");
                        break;
                    case "System.Data.OleDb": 
                        q = new SqlStatement("StoreProcedure1");
                        break;
                    case "System.Data.Odbc": 
                        q = new SqlStatement("{{ {preturn} = call StoreProcedure1 ({pinput}, {poutput}) }}"); ;
                        break;
                    default:
                        q = null;
                        break;
                }
                if(q!=null)
                {
                    q.CommandType = CommandType.StoredProcedure;
                    // Since there is no refernce to parameters in CommandText
                    // So the order of placing does matter here.
                    q.PlaceParameter("preturn", new ParameterInfo() { DbType = DbType.Int32, Direction = ParameterDirection.ReturnValue });
                    q.PlaceParameter("pinput", 123);
                    q.PlaceParameter("poutput", new ParameterInfo() { DbType = DbType.AnsiString, Size = 50, Direction = ParameterDirection.Output });
                    cmd = q.MakeCommand(conn);
                    conn.ExecuteNonQuery(q, trans);
                    Assert.AreEqual(31, q.ParameterValue("preturn"));
                    Assert.AreEqual(123, q.ParameterValue("pinput"));
                    Assert.AreEqual("((123))", q.ParameterValue("poutput"));
                }

                conn.ExecuteNonQuery(new SqlStatement("drop procedure [StoreProcedure1]"), trans);

                conn.ExecuteNonQuery(new SqlStatement("drop table #Test"), trans);

                trans.Rollback();

            }
        }

        [TestMethod]
        public void TestDefaultFactory()
        {
            try
            {
                var q = new SqlStatement("");
                q.MakeCommand();
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.SqlCommandFactory));
                q = new SqlStatement("");
                q.MakeCommand();
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.SqlCommandFactory));

                SqlStatement.DefaultCommandFactory = new SqlPlace.Factories.OleDbCommandFactory();
                q = new SqlStatement("");
                q.MakeCommand();
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.OleDbCommandFactory));
                q = new SqlStatement("");
                q.MakeCommand();
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.OleDbCommandFactory));

                q = new SqlStatement("");
                q.CommandFactory = new SqlPlace.Factories.OdbcCommandFactory();
                q.MakeCommand();
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.OdbcCommandFactory));
                Assert.IsTrue(SqlStatement.DefaultCommandFactory.GetType() == typeof(SqlPlace.Factories.OleDbCommandFactory));

                DbConnection conn = new System.Data.SqlClient.SqlConnection();
                q = new SqlStatement("");
                q.MakeCommand(conn);
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.SqlCommandFactory));
                conn = new System.Data.OleDb.OleDbConnection();
                q = new SqlStatement("");
                q.MakeCommand(conn);
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.OleDbCommandFactory));
                conn = new System.Data.Odbc.OdbcConnection();
                q = new SqlStatement("");
                q.MakeCommand(conn);
                Assert.IsTrue(q.CommandFactory.GetType() == typeof(SqlPlace.Factories.OdbcCommandFactory));
            }
            finally
            {
                SqlStatement.DefaultCommandFactory = new SqlPlace.Factories.SqlCommandFactory();
            }
        }

        [TestMethod]
        public void TestMethodTodo()
        {
            // TODO Clone (for slightly different)

            // TODO Data provider specific replacement

            // TODO Should it throw exception on Make if found no parameter assignment ?

            // TODO Binary

            // TODO Make sure that Make produce correct parameter name

            // TODO CommandInfo.ParameterDictionary

        }

    }

}
