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
        public Boolean entered = false;
        public Boolean longEnough = false;
        public Boolean notTooLong = false;
    }
    public class LastName
    {
        public Boolean entered = false;
        public Boolean longEnough = false;
        public Boolean notTooLong = false;
    }
    public class Email
    {
        public Boolean entered = false;
        public Boolean unique = false;
        public Boolean valid = false;
    }
    public class Password
    {
        public Boolean entered = false;
        public Boolean longEnough = false;
        public Boolean containsNumber = false;
        public Boolean containsLetter = false;
    }
    public class PasswordVerify
    {
        public Boolean entered = false;
        public Boolean sameAsPassword = false;
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

        if (!String.IsNullOrEmpty(firstName))
        {
            requirements.firstName.entered = true;
            if (firstName.Length > 1) requirements.firstName.longEnough = true;
            if (firstName.Length <= 50) requirements.firstName.notTooLong = true;
        }
        if (!String.IsNullOrEmpty(lastName))
        {
            requirements.lastName.entered = true;
            if (lastName.Length > 1) requirements.lastName.longEnough = true;
            if (lastName.Length <= 50) requirements.lastName.notTooLong = true;
        }
        if (!String.IsNullOrEmpty(email))
        {
            requirements.email.entered = true;
            cmd = new("select * from `users` where email = @email", conn);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Prepare();
            reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                requirements.email.unique = true;
            }
            conn.Close();
            if (IsValidEmail(email)) requirements.email.valid = true;
        }
        if (!String.IsNullOrEmpty(password))
        {
            requirements.password.entered = true;
            if (password.Length >= 6) requirements.password.longEnough = true;
            if (password.Any(char.IsDigit)) requirements.password.containsNumber = true;
            if (password.Any(char.IsLetter)) requirements.password.containsLetter = true;
        }
        if (!String.IsNullOrEmpty(passwordVerify))
        {
            requirements.passwordVerify.entered = true;
            if (passwordVerify == password) requirements.passwordVerify.sameAsPassword = true;
        }

        if (!(
            requirements.firstName.entered &&
            requirements.firstName.longEnough &&
            requirements.firstName.notTooLong &&
            requirements.lastName.entered &&
            requirements.lastName.longEnough &&
            requirements.lastName.notTooLong &&
            requirements.email.entered &&
            requirements.email.unique &&
            requirements.email.valid &&
            requirements.password.entered &&
            requirements.password.containsLetter &&
            requirements.password.containsNumber &&
            requirements.password.longEnough &&
            requirements.passwordVerify.entered &&
            requirements.passwordVerify.sameAsPassword
        ))
        {
            return Conflict(JsonConvert.SerializeObject(requirements));
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
