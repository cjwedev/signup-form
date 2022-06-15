namespace SignUp
{
    using System;
    using MySql.Data.MySqlClient;

    public static class SignUp
    {
        public static void Main()
        {
            MySqlConnection conn = new("server=92.63.169.233;uid=dirk;pwd=Debian2DB!;database=projects");
            conn.Open();
            //MySqlCommand cmd = new("select * from `users`", conn);
            //MySqlDataReader reader = cmd.ExecuteReader();

            //while (reader.Read())
            //{
            //    String row = "|";
            //    for (Int32 i = 0; i < reader.FieldCount - 1; i++)
            //    {
            //        row += reader.GetString(i) + "|";
            //    }
            //    Console.WriteLine(row);
            //}


            conn.Close();
        }
    }
}