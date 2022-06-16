using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Dynamic;
using System.Reflection;

namespace backend.Controllers;

// const reqrmts = {
//         user: {
//             entered: false,
//             unique: false,
//             longEnough: false,
//             notTooLong: false
//         },
//         email: {
//             entered: false,
//             unique: false,
//             valid: false
//         },
//         pass: {
//             entered: false,
//             longEnough: false,
//             smallLetter: false,
//             capitalLetter: false,
//             number: false,
//             specialChar: false
//         },
//         passVerify: {
//             entered: false,
//             equal: false
//         }
//     }

class Requirements
{
    public (Boolean Entered, Boolean LongEnough, Boolean NotTooLong) firstName = (false, false, false);
    public (Boolean Entered, Boolean LongEnough, Boolean NotTooLong) lastName = (false, false, false);
    public (Boolean Entered, Boolean Unique, Boolean Valid) email = (false, false, false);
    public (Boolean Entered, Boolean LongEnough, Boolean ContainsNumber, Boolean ContainsLetter) password = (false, false, false, false);
    public (Boolean Entered, Boolean SameAsPassword) passwordVerify = (false, false);
    // public class FirstName
    // {
    //     public Boolean Entered = false;
    //     public Boolean LongEnough = false;
    //     public Boolean NotTooLong = false;
    // }
    // public class LastName
    // {
    //     public Boolean Entered = false;
    //     public Boolean LongEnough = false;
    //     public Boolean NotTooLong = false;
    // }
    // public class Email
    // {
    //     public Boolean Entered = false;
    //     public Boolean Unique = false;
    //     public Boolean Valid = false;
    // }
    // public class Password
    // {
    //     public Boolean Entered = false;
    //     public Boolean LongEnough = false;
    //     public Boolean ContainsNumber = false;
    //     public Boolean ContainsLetter = false;
    // }
    // public class PasswordVerify
    // {
    //     public Boolean Entered = false;
    //     public Boolean SameAsPassword = false;
    // }
}


[ApiController]
[Route("[controller]")]
public class RegisterController : ControllerBase
{
    [HttpPost(Name = "register")]
    public IActionResult Post(String firstName, String lastName, String email, String password, String passwordVerify)
    {
        MySqlCommand cmd = new();
        MySqlDataReader reader = null;

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

        Boolean allRequirementsMet = true;
        foreach (FieldInfo field in typeof(Requirements).GetFields())
        {
            foreach (FieldInfo subField in field.FieldType.GetFields())
            {
                if (!(Boolean)(subField.GetValue(subField))!)
                {
                    allRequirementsMet = false;
                    break;
                }
            }
        }

        if (!allRequirementsMet)
        {
            return Unauthorized(JsonConvert.SerializeObject(requirements));
        }

        String hash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
        conn.Open();
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
