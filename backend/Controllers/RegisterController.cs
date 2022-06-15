using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Net.Mail;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
public class RegisterController : ControllerBase
{
    [HttpPost(Name = "Register")]
    public IActionResult Post(String firstName, String lastName, String email, String password)
    {
        List<String> errors = new();
        if (firstName.Length == 0) errors.Add("No first name entered");
        else if (firstName.Length < 2) errors.Add("First name too short");
        else if (firstName.Length > 50) errors.Add("First name too long");

        if (lastName.Length == 0) errors.Add("No last name entered");
        else if (lastName.Length < 2) errors.Add("Last name too short");
        else if (lastName.Length > 50) errors.Add("Last name too long");

        if (email.Length == 0) errors.Add("No email entered");
        else if (!IsValidEmail(email)) errors.Add("Email is invalid");
        else if (email.Length > 50) errors.Add("Email too long");

        if (password.Length == 0) errors.Add("No password entered");
        else
        {
            if (password.Length < 6) errors.Add("Password not long enough");
            if (!password.Any(char.IsDigit)) errors.Add("Password doesn't contain a digit");
            if (!password.Any(char.IsLetter)) errors.Add("Password doesn't contain a letter");
        }

        if (errors.Count > 0)
        {
            return Unauthorized(errors);
        }

        DotNetEnv.Env.Load();
        String hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
        String server = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_SERVER")!;
        String database = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_DATABASE")!;
        String uid = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_UID")!;
        String pwd = Environment.GetEnvironmentVariable("PROJECTS_DB_CONNECTION_PWD")!;
        MySqlConnection conn = new($"server={server};database={database};uid={uid};pwd={pwd}");
        conn.Open();
        MySqlCommand cmd = new("insert into users (`first-name`,`last-name`,email,password) values (@firstName,@lastName,@email,@hash)", conn);
        cmd.Parameters.AddWithValue("@firstName", firstName);
        cmd.Parameters.AddWithValue("@lastName", lastName);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@hash", hash);
        cmd.Prepare();
        cmd.ExecuteNonQuery();

        cmd = new("select * from `users` where email = @email", conn);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Prepare();
        MySqlDataReader reader = cmd.ExecuteReader();
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
