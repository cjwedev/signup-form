import { useState } from "react";
import './Register.css';
import axios from "axios";
import { useNavigate } from 'react-router-dom';

export default function SignUp() {
    const navigate = useNavigate();

    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [email, setEmail] = useState("");
    const [pass, setPass] = useState("");
    const [passVerify, setPassVerify] = useState("");

    const [firstNameErr, setFirstNameErr] = useState("");
    const [lastNameErr, setLastNameErr] = useState("");
    const [emailErr, setEmailErr] = useState("");
    const [passErr, setPassErr] = useState("");
    const [passVerifyErr, setPassVerifyErr] = useState("");
    const [globalErr, setGlobalErr] = useState("");

    document.title = "Registreren";

    const handleSubmit = async (e: any) => {
        e.preventDefault();

        let firstNameErr = "";
        let lastNameErr = "";
        let emailErr = "";
        let passErr = "";
        let passwordVerifyErr = "";
        let globalErr = "";

        await axios.post(`http://localhost:5000/Register`, null, {
            params: {
                firstName: firstName,
                lastName: lastName,
                email: email,
                password: pass,
                passwordVerify: passVerify
            }
        }).then(() => {
            navigate("/successful");
        }).catch(function (error) {
            if (error.response) {
                switch (error.response.status) {
                    case 500: {
                        globalErr = "Er ging iets fout bij het toevoegen van uw account aan de database. Probeer het later opnieuw.";
                        break;
                    }
                    case 409: {
                        const data = error.response.data;

                        //First name
                        if (!data.firstName.entered) firstNameErr += "Vul een voornaam in.\n";
                        else {
                            if (!data.firstName.longEnough) firstNameErr += "Deze voornaam is niet lang genoeg. Een voornaam moet minimaal 2 karakters lang zijn.\n";
                            if (!data.firstName.notTooLong) firstNameErr += "Deze voornaam is te lang. Een voornaam kan maximaal 50 karakters lang zijn.\n";
                        }
                        //Last name
                        if (!data.lastName.entered) lastNameErr += "Vul een achternaam in.\n";
                        else {
                            if (!data.lastName.longEnough) lastNameErr += "Deze achternaam is niet lang genoeg. Een gebruikernaam moet minimaal 2 karakters lang zijn.\n";
                            if (!data.lastName.notTooLong) lastNameErr += "Deze achternaam is te lang. Een gebruikernaam kan maximaal 50 karakters lang zijn.\n";
                        }
                        //Email
                        if (!data.email.entered) emailErr += "Vul een email-adres in.\n";
                        else {
                            if (!data.email.unique) emailErr += "Er bestaat al een account dat dit email-adres gebruikt.\n";
                            if (!data.email.valid) emailErr += "Deze email is niet valide\n";
                        }
                        //Password
                        if (!data.password.entered) passErr += "Vul een wachtwoord in.\n";
                        else {
                            if (!data.password.longEnough) passErr += "Je wachtwoord is niet lang genoeg. Een wachtwoord moet minimaal 8 karakters lang zijn.\n";
                            if (!data.password.containsLetter) passErr += "Je wachtwoord moet minimaal 1 letter bevatten.\n";
                            if (!data.password.containsNumber) passErr += "Je wachtwoord moet minimaal 1 cijfer bevatten.\n";
                        }
                        //Password verify
                        if (!data.passwordVerify.entered) passwordVerifyErr += "Vul hetzelfde wachtwoord in als hierboven. Dit is belangrijk zodat u geen typfout maakt in uw wachtwoord.\n";
                        else {
                            if (!data.passwordVerify.sameAsPassword) passwordVerifyErr += "Dit wachtwoord is niet gelijk aan het bovenstaande wachtwoord.\n";
                        }
                        break;
                    }
                    case 200: {
                        console.log(`Account succesvol aangemaakt!`);
                        break;
                    }
                    default: {
                        globalErr = `Ho, er ging iets mis. Sorry! (${error.response.status})`;
                        break;
                    }
                }
            } else if (error.request) {
                console.log(`No response gotten ${error.request}`);
            } else {
                console.log('Error', error.message);
            }
        });

        setFirstNameErr(firstNameErr);
        setLastNameErr(lastNameErr);
        setEmailErr(emailErr);
        setPassErr(passErr);
        setPassVerifyErr(passwordVerifyErr);
        setGlobalErr(globalErr);
    }

    return (
        <>
            <div className="field">
                <h1 className="header">Registreren</h1>
                <form className="form" onSubmit={handleSubmit}>
                    <label>
                        Voornaam:<br></br>
                        <input type="text" name="firstName" placeholder="Voornaam"
                            onChange={(e) => { setFirstName(e.target.value); setFirstNameErr("") }} /><br></br>
                        <span className="error">{firstNameErr}</span>
                    </label>
                    <label>
                        Achternaam:<br></br>
                        <input type="text" name="lastName" placeholder="Achternaam"
                            onChange={(e) => { setLastName(e.target.value); setLastNameErr("") }} /><br></br>
                        <span className="error">{lastNameErr}</span>
                    </label>
                    <label>
                        Email:<br></br>
                        <input type="text" name="email" placeholder="Email"
                            onChange={(e) => { setEmail(e.target.value); setEmailErr("") }} /><br></br>
                        <span className="error">{emailErr}</span>
                    </label>
                    <label>
                        Wachtwoord:<br></br>
                        <input type="password" name="pass" placeholder="Wachtwoord"
                            onChange={(e) => { setPass(e.target.value); setPassErr("") }} /><br></br>
                        <span className="error">{passErr}</span>
                    </label>
                    <label>
                        Wachtwoord herhalen:<br></br>
                        <input type="password" name="pass2" placeholder="Wachtwoord herhalen"
                            onChange={(e) => { setPassVerify(e.target.value); setPassVerifyErr("") }} /><br></br>
                        <span className="error">{passVerifyErr}</span>
                    </label>
                    <input type="submit" name="submit" value="Registreren" /><br></br>
                    <span className="final error">{globalErr}</span><br></br>
                </form>
            </div>
        </>
    )
}