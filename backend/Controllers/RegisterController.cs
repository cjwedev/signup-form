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
        Errors errors = new Errors();
        if (firstName.Length == 0) errors.firstName.Add("No first name entered");
        else if (firstName.Length < 2) errors.firstName.Add("First name too short");
        else if (firstName.Length > 50) errors.firstName.Add("First name too long");

        if (lastName.Length == 0) errors.lastName.Add("No last name entered");
        else if (lastName.Length < 2) errors.lastName.Add("Last name too short");
        else if (lastName.Length > 50) errors.lastName.Add("Last name too long");

        if (email.Length == 0) errors.email.Add("No email entered");
        else if (!IsValidEmail(email)) errors.email.Add("Email is invalid");
        else if (email.Length > 50) errors.email.Add("Email too long");

        if (password.Length == 0) errors.password.Add("No password entered");
        else
        {
            if (password.Length < 6) errors.password.Add("Password not long enough");
            if (!password.Any(char.IsDigit)) errors.password.Add("Password doesn't contain a digit");
            if (!password.Any(char.IsLetter)) errors.password.Add("Password doesn't contain a letter");
        }

        if (errors.firstName.Count + errors.lastName.Count + errors.email.Count + errors.password.Count > 0)
        {
            return Unauthorized(JsonConvert.SerializeObject(errors));
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

class Errors
{
    public List<String> firstName = new();
    public List<String> lastName = new();
    public List<String> email = new();
    public List<String> password = new();
}