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

        [TestMethod]
        public void TestMethod1()
        {
            SqlStatement q;
            DbCommand cmd;

            string xName = "John";
            DateTime xCreateDate = new DateTime(2010, 1, 3);
            int xStatus = 3;
            DateTime? xUpdateDate = null;

            // Basic usage
            q = new SqlStatement("select * from User where name={3} and create_date={2} and status={1} and update_date={0}",
                xUpdateDate, xStatus, xCreateDate, xName);
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@p3 and create_date=@p2 and status=@p1 and update_date=@p0", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(SqlDbType.NVarChar, (cmd.Parameters["@p3"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p1"].Value);
            Assert.IsNull(cmd.Parameters["@p0"].Value);

            // Detailed parameters
            q = new SqlStatement("select * from User where name={3} and create_date={2} and status={1} and update_date={0}",
                xUpdateDate, xStatus, xCreateDate, new ParameterInfo(xName, SqlDbType.VarChar));
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@p3 and create_date=@p2 and status=@p1 and update_date=@p0", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@p3"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p1"].Value);
            Assert.IsNull(cmd.Parameters["@p0"].Value);

            // Placing local parameters individually
            q = new SqlStatement("select * from User where name={3} and create_date={2} and status={1} and update_date={0}");
            q.PlaceParameter(0, xUpdateDate);
            q.PlaceParameter(1, xStatus);
            q.PlaceParameter(2, xCreateDate);
            q.PlaceParameter(3, new ParameterInfo(xName, SqlDbType.VarChar));
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@p3 and create_date=@p2 and status=@p1 and update_date=@p0", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@p3"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p1"].Value);
            Assert.IsNull(cmd.Parameters["@p0"].Value);

            // Named (global) parameters
            q = new SqlStatement("select * from User where name={N} and create_date={C} and status={S} and update_date={U}",
                new Dictionary<string, object> { { "U", xUpdateDate }, { "S", xStatus }, { "C", xCreateDate }, { "N", new ParameterInfo(xName, SqlDbType.VarChar) } });
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@N and create_date=@C and status=@S and update_date=@U", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@N"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@N"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@C"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@S"].Value);
            Assert.IsNull(cmd.Parameters["@U"].Value);

            // Placing local and global parameters
            q = new SqlStatement("select * from User where name={N} and create_date={C} and status={0} and update_date={1}", xStatus, xUpdateDate);
            q.PlaceParameter("C", xCreateDate);
            q.PlaceParameter("N", new ParameterInfo(xName, SqlDbType.VarChar));
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@N and create_date=@C and status=@p0 and update_date=@p1", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@N"].Value);
            Assert.AreEqual(SqlDbType.VarChar, (cmd.Parameters["@N"] as SqlParameter).SqlDbType);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@C"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p0"].Value);
            Assert.IsNull(cmd.Parameters["@p1"].Value);

            // Placing Sub (Nested query)
            q = new SqlStatement("select * from User where name={N} and create_date={C} and {MORE}");
            q.PlaceStatement("MORE", "status={0} and update_date={1}", xStatus, xUpdateDate);
            q.PlaceParameter("C", xCreateDate);
            q.PlaceParameter("N", xName);
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@N and create_date=@C and status=@p0 and update_date=@p1", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@N"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@C"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p0"].Value);
            Assert.IsNull(cmd.Parameters["@p1"].Value);

            // Mixed local and global parameter with subquery
            q = new SqlStatement("select * from User where name={0} and {MORE} and update_date={U}");
            //q.PlaceSql("MORE", "create_date={0} and status={S}", xCreateDate);
            q.PlaceStatement("MORE", new SqlStatement("create_date={0} and status={S}", xCreateDate));
            q.PlaceParameter("U", xUpdateDate);
            q.PlaceParameter("S", xStatus);
            q.PlaceParameter(0, xName);
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@p0 and create_date=@p1 and status=@S and update_date=@U", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@S"].Value);
            Assert.IsNull(cmd.Parameters["@U"].Value);

            // Global parameter can be placed in subquery
            q = new SqlStatement("select * from User where name={0} and {MORE} and update_date={U}");
            q.PlaceParameter(0, xName);
            q.PlaceParameter("U", xUpdateDate);
            q.PlaceStatement("MORE", new SqlStatement("create_date={0} and status={S}", xCreateDate)).PlaceParameter("S", xStatus);
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@p0 and create_date=@p1 and status=@S and update_date=@U", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@S"].Value);
            Assert.IsNull(cmd.Parameters["@U"].Value);

            q = new SqlStatement("{A}-{1}-{0}-{MORE1}-{B}-{MORE2}", 10, 11);
            q.PlaceStatement("MORE1", "({0}+{A}+{B}+{1}+{MORE3})", 21, 22);
            q.PlaceStatement("MORE2", "({MORE3}*{0}*{C})", 31);
            q.PlaceStatement("MORE3", "({D}/{0})", 41);
            q.PlaceParameter("A", "a");
            q.PlaceParameter("B", "b");
            q.PlaceParameter("C", "c");
            q.PlaceParameter("D", "d");
            cmd = q.ToCommand();
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

            // Place list
            q = new SqlStatement("select * from User where {CONDS}");
            var cond = q.PlaceList("CONDS", new SqlList(" and ", "(1=1)"));
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where (1=1)", cmd.CommandText);
            cond.Add("name={0}", xName);
            cond.Add("create_date={0}", xCreateDate);
            cond.Add("status={0}", xStatus);
            cond.Add("update_date={0}", xUpdateDate);
            cmd = q.ToCommand();
            Assert.AreEqual("select * from User where name=@p0 and create_date=@p1 and status=@p2 and update_date=@p3", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p2"].Value);
            Assert.IsNull(cmd.Parameters["@p3"].Value);

            // Place And/Comma Collection
            q = new SqlStatement("select {FLDS} from User where {CONDS}");
            cond = q.PlaceList("CONDS", SqlList.AndClauses());
            cond.Add("name={0}", xName);
            cond.Add("create_date={0}", xCreateDate);
            cond.Add("status={0}", xStatus);
            cond.Add("update_date={0}", xUpdateDate);
            var flds = q.PlaceList("FLDS", SqlList.CommaClauses());
            flds.Add("f1");
            flds.Add("f2");
            flds.Add("f3");
            cmd = q.ToCommand();
            Assert.AreEqual("select f1, f2, f3 from User where name=@p0 and create_date=@p1 and status=@p2 and update_date=@p3", cmd.CommandText);
            Assert.AreEqual(xName, cmd.Parameters["@p0"].Value);
            Assert.AreEqual(xCreateDate, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(xStatus, cmd.Parameters["@p2"].Value);
            Assert.IsNull(cmd.Parameters["@p3"].Value);

            // Relative token in collection, Absolute token in collection
            var fields = new Dictionary<string, object>() { { "name={0}", "AAA" }, { "gender={0}", 1 } };
            var conditions = new Dictionary<string, object>() { { "section={0}", 2 }, { "status={0}", 3 }, { "region=@R", 99 }, { "country='TH'", null } };
            q = new SqlStatement("update Table1 set {FLDS} where {CONDS}");
            // TODO There should be some ObjectUtils to automatically get Property Names and Values from an object
            q.PlaceList("FLDS", SqlList.CommaClauses()).AddRange(fields);
            q.PlaceList("CONDS", SqlList.AndClauses()).AddRange(conditions);
            q.PlaceParameter("R", 98);
            cmd = q.ToCommand();
            Assert.AreEqual("update Table1 set name=@p0, gender=@p1 where section=@p2 and status=@p3 and region=@R and country='TH'", cmd.CommandText);
            Assert.AreEqual("AAA", cmd.Parameters["@p0"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(2, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(98, cmd.Parameters["@R"].Value);

            var assigns = new Dictionary<string, object>() { { "name", "AAA" }, { "gender", 1 } };
            q = new SqlStatement("update Table1 set {FLDS} where {CONDS}");
            q.PlaceList("FLDS", SqlList.CommaAssignments(assigns));
            q.PlaceList("CONDS", SqlList.AndClauses()).AddRange(conditions);
            q.PlaceParameter("R", 98);
            cmd = q.ToCommand();
            Assert.AreEqual("update Table1 set name=@p0, gender=@p1 where section=@p2 and status=@p3 and region=@R and country='TH'", cmd.CommandText);
            Assert.AreEqual("AAA", cmd.Parameters["@p0"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p1"].Value);
            Assert.AreEqual(2, cmd.Parameters["@p2"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(98, cmd.Parameters["@R"].Value);

            // Comma Values
            object[] set1 = { new ParameterInfo("1", SqlDbType.VarChar), "3", "5" };
            object[] set2 = { new ParameterInfo(1, SqlDbType.SmallInt), 3, 5 };
            q = new SqlStatement("select * from T1 where F1 in ({SET1}) and F2 in ({SET2})");
            q.PlaceList("SET1", SqlList.CommaValues(set1));
            q.PlaceList("SET2", SqlList.CommaValues(set2));
            cmd = q.ToCommand();
            Assert.AreEqual("select * from T1 where F1 in (@p0, @p1, @p2) and F2 in (@p3, @p4, @p5)", cmd.CommandText);
            Assert.AreEqual("1", cmd.Parameters["@p0"].Value);
            Assert.AreEqual("3", cmd.Parameters["@p1"].Value);
            Assert.AreEqual("5", cmd.Parameters["@p2"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(5, cmd.Parameters["@p5"].Value);

            // You can even place query instead of parameter
            q = new SqlStatement("select * from T1 where F1 in ({0}) and F2 in ({X})", SqlList.CommaValues(set1));
            q.PlaceParameter("X", SqlList.CommaValues(set2));
            cmd = q.ToCommand();
            cmd = q.ToCommand(); // Do it again should yield the same SQL
            cmd = q.ToCommand();
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
            cmd = q.ToCommand();
            cmd = q.ToCommand(); // Do it again should yield the same SQL
            cmd = q.ToCommand();
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
            cmd = q.ToCommand();
            cmd = q.ToCommand(); // Do it again should yield the same SQL
            cmd = q.ToCommand();
            Assert.AreEqual("select * from T1 where F1 in (@p0, @p1, @p2) and F2 in (@p3, @p4, @p5)", cmd.CommandText);
            Assert.AreEqual("1", cmd.Parameters["@p0"].Value);
            Assert.AreEqual("3", cmd.Parameters["@p1"].Value);
            Assert.AreEqual("5", cmd.Parameters["@p2"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(5, cmd.Parameters["@p5"].Value);

            // ToString
            Assert.AreEqual("select * from T1 where F1 in ('1', '3', '5') and F2 in (1, 3, 5)", q.PlainText());

            // Store Procedure = Command without {tag} but has named parameter
            q = new SqlStatement("sp_StoreProc") { CommandType = CommandType.StoredProcedure };
            q.PlaceParameter("A", 1);
            q.PlaceParameter("B", new ParameterInfo(10, SqlDbType.Int) { Direction = ParameterDirection.Output });
            cmd = q.ToCommand();
            Assert.AreEqual("sp_StoreProc", cmd.CommandText);
            Assert.AreEqual(CommandType.StoredProcedure, cmd.CommandType);
            Assert.AreEqual(1, cmd.Parameters["@A"].Value);
            Assert.AreEqual(ParameterDirection.Input, cmd.Parameters["@A"].Direction);
            Assert.AreEqual(10, cmd.Parameters["@B"].Value);
            Assert.AreEqual(ParameterDirection.Output, cmd.Parameters["@B"].Direction);

            // TODO Dont process JSON { }, Escape {{ }}

            // TODO Clone (for slightly different)

            // TODO Test the "TOP 10"

            // TODO Publicize the "Make"

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


        [TestMethod]
        public void TestPlaceListAsParameter()
        {
            object[] set1 = { new ParameterInfo("1", SqlDbType.VarChar), "3", "5" };
            object[] set2 = { new ParameterInfo(1, SqlDbType.SmallInt), 3, 5 };

            var q = new SqlStatement("select * from T1 where {FILTER}");
            q.PlaceStatement("FILTER", "F1 in ({A}) and F2 in ({B})", new Dictionary<string, object>() { { "A", SqlList.CommaValues(set1) }, { "B", SqlList.CommaValues(set2) } });
            var cmd = q.ToCommand();
            cmd = q.ToCommand(); // Do it again should yield the same SQL
            cmd = q.ToCommand();
            Assert.AreEqual("select * from T1 where F1 in (@p0, @p1, @p2) and F2 in (@p3, @p4, @p5)", cmd.CommandText);
            Assert.AreEqual("1", cmd.Parameters["@p0"].Value);
            Assert.AreEqual("3", cmd.Parameters["@p1"].Value);
            Assert.AreEqual("5", cmd.Parameters["@p2"].Value);
            Assert.AreEqual(1, cmd.Parameters["@p3"].Value);
            Assert.AreEqual(3, cmd.Parameters["@p4"].Value);
            Assert.AreEqual(5, cmd.Parameters["@p5"].Value);
        }

    }

}
