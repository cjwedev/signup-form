using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Net.Mail;

namespace backend.Controllers;

class Requirements
{
    public FirstName firstName = new();
    public LastName lastName = new();
    public Email email = new();
    public Password password = new();
    public PasswordVerify passwordVerify = new();

    public class FirstName
    {
        public Boolean Entered = false;
        public Boolean LongEnough = false;
        public Boolean NotTooLong = false;
    }
    public class LastName
    {
        public Boolean Entered = false;
        public Boolean LongEnough = false;
        public Boolean NotTooLong = false;
    }
    public class Email
    {
        public Boolean Entered = false;
        public Boolean Unique = false;
        public Boolean Valid = false;
    }
    public class Password
    {
        public Boolean Entered = false;
        public Boolean LongEnough = false;
        public Boolean ContainsNumber = false;
        public Boolean ContainsLetter = false;
    }
    public class PasswordVerify
    {
        public Boolean Entered = false;
        public Boolean SameAsPassword = false;
    }
}


[ApiController]
[Route("[controller]")]
public class RegisterController : ControllerBase
{
    [HttpPost(Name = "register")]
    public IActionResult Post(String firstName, String lastName, String email, String password, String passwordVerify)
    {
        MySqlCommand cmd = new();
        MySqlDataReader reader;

        Requirements requirements = new();

        DotNetEnv.Env.Load();
        String server = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_SERVER")!;
        String database = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_DATABASE")!;
        String uid = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_UID")!;
        String pwd = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_PWD")!;
        MySqlConnection conn = new($"server={server};database={database};uid={uid};pwd={pwd}");
        conn.Open();

        if (firstName != "")
        {
            requirements.firstName.Entered = true;
            if (firstName.Length > 1) requirements.firstName.LongEnough = true;
            if (firstName.Length <= 50) requirements.firstName.NotTooLong = true;
        }
        if (lastName != "")
        {
            requirements.lastName.Entered = true;
            if (lastName.Length > 1) requirements.lastName.LongEnough = true;
            if (lastName.Length <= 50) requirements.lastName.NotTooLong = true;
        }
        if (email != "")
        {
            requirements.email.Entered = true;
            cmd = new("select * from `users` where email = @email", conn);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Prepare();
            reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                requirements.email.Unique = true;
            }
            conn.Close();
            if (IsValidEmail(email)) requirements.email.Valid = true;
        }
        if (password != "")
        {
            requirements.password.Entered = true;
            if (password.Length >= 6) requirements.password.LongEnough = true;
            if (password.Any(char.IsDigit)) requirements.password.ContainsNumber = true;
            if (password.Any(char.IsLetter)) requirements.password.ContainsLetter = true;
        }
        if (passwordVerify != "")
        {
            requirements.passwordVerify.Entered = true;
            if (passwordVerify == password) requirements.passwordVerify.SameAsPassword = true;
        }

        if (!(
            requirements.firstName.Entered &&
            requirements.firstName.LongEnough &&
            requirements.firstName.NotTooLong &&
            requirements.lastName.Entered &&
            requirements.lastName.LongEnough &&
            requirements.lastName.NotTooLong &&
            requirements.email.Entered &&
            requirements.email.Unique &&
            requirements.email.Valid &&
            requirements.password.Entered &&
            requirements.password.ContainsLetter &&
            requirements.password.ContainsNumber &&
            requirements.password.LongEnough &&
            requirements.passwordVerify.Entered &&
            requirements.passwordVerify.SameAsPassword
        ))
        {
            return Unauthorized(JsonConvert.SerializeObject(requirements));
        }

        conn.Open();
        String hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
        cmd = new("insert into users (`first-name`,`last-name`,email,password) values (@firstName,@lastName,@email,@hash)", conn);
        cmd.Parameters.AddWithValue("@firstName", firstName);
        cmd.Parameters.AddWithValue("@lastName", lastName);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Prepare();
        cmd.ExecuteNonQuery();

        cmd = new("select * from `users` where email = @email", conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Prepare();
        reader = cmd.ExecuteReader();
        Boolean success = reader.Read();
        conn.Close();
        if (success)
        {
            return Ok();
        }
        else
        {
            return this.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static Boolean IsValidEmail(String email)
    {
        String trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
        {
            return false;
        }
        try
        {
            MailAddress addr = new MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }

    public static String GetAllData(MySqlDataReader reader)
    {
        String result = "";

        List<String> columns = new();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        while (reader.Read())
        {
            String row = "|";
            foreach (String column in columns)
            {
                row += reader[column] + "|";
            }
            result += row + "\n";
        }
        return result;
    }
}
