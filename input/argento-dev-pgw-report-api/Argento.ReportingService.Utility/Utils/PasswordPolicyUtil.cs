using System.Text.RegularExpressions;

namespace Argento.ReportingService.Utility.Utils
{
    /// <summary>
    /// PasswordPolicyUtil
    /// Author : Peeradis S.
    /// Date 18/5/2018
    ///
    /// Check Password Strength and return Score  
    /// Example : CheckPasswordStrength("ArcadiaSCG!123")
    /// </summary>
    public static class PasswordPolicyUtil
    {
        public static int CheckPasswordStrength(string password)
        {
            int score = 0;


            if (Regex.Match(password, @"\d", RegexOptions.ECMAScript).Success)
            {
                score++;
            }
            if (Regex.Match(password, "[a-z]", RegexOptions.ECMAScript).Success)
            {
                score++;
            }
            if (Regex.Match(password, "[A-Z]", RegexOptions.ECMAScript).Success)
            {
                score++;
            }
            if (Regex.Match(password, "[!,@,#,$,%,^,&,*,(,),~,-,_]", RegexOptions.ECMAScript).Success)
            {
                score++;
            }

            return score;

        }


    }
}