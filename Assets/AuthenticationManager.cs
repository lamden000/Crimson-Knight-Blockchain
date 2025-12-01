using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AuthenticationManager : MonoBehaviour
{
    public TMP_InputField emailField;
    public TMP_InputField passField;
    public TMP_InputField userField;
    public TextMeshProUGUI statusText;

    private enum AuthMode { Login, Register }
    private AuthMode mode = AuthMode.Login;

    private bool usernameRequired = false; // Để biết userField đã bật hay chưa

    private void Start()
    {
        // Mặc định login mode
        userField.gameObject.SetActive(false);
    }

    // -----------------------------
    //          REGISTER
    // -----------------------------
    public void OnPressRegister()
    {
        if (!usernameRequired)
        {
            // Lần bấm đầu tiên → bật ô username
            mode = AuthMode.Register;
            usernameRequired = true;

            userField.gameObject.SetActive(true);
            DisplayStatus("Please enter a username to continue.", true);
            return;
        }

        // Kiểm tra đủ dữ liệu chưa
        if (string.IsNullOrEmpty(emailField.text))
        {
            DisplayStatus("Email is required.", true);
            return;
        }
        if (string.IsNullOrEmpty(passField.text))
        {
            DisplayStatus("Password is required.", true);
            return;
        }
        if (string.IsNullOrEmpty(userField.text))
        {
            DisplayStatus("Username is required.", true);
            return;
        }

        Register(emailField.text, passField.text, userField.text);
    }

    public void Register(string email, string password, string username)
    {
        var request = new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = username
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            OnRegisterSuccess,
            OnRegisterFailure
        );
    }

    void OnRegisterSuccess(RegisterPlayFabUserResult result)
    {
        DisplayStatus("Registration successful! Welcome " + result.Username, false);
        Debug.Log("Register OK — " + result.Username);

        // Reset UI
        usernameRequired = false;
        userField.gameObject.SetActive(false);
    }

    void OnRegisterFailure(PlayFabError error)
    {
        DisplayStatus("Register failed: " + error.ErrorMessage, true);
        Debug.LogError("Register FAIL:\n" + error.GenerateErrorReport());
    }


    // -----------------------------
    //          LOGIN
    // -----------------------------
    public void OnPressLogin()
    {
        if (userField.gameObject.activeSelf)
        {
            // Ẩn username field nếu đang hiện (vì login không cần)
            userField.gameObject.SetActive(false);
            usernameRequired = false;
            mode = AuthMode.Login;

            DisplayStatus("Login mode.", false);
            return;
        }

        // Validate input
        if (string.IsNullOrEmpty(emailField.text))
        {
            DisplayStatus("Email is required.", true);
            return;
        }
        if (string.IsNullOrEmpty(passField.text))
        {
            DisplayStatus("Password is required.", true);
            return;
        }

        Login(emailField.text, passField.text);
    }

    public void Login(string email, string password)
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password
        };

        PlayFabClientAPI.LoginWithEmailAddress(
            request,
            OnLoginSuccess,
            OnLoginFailure
        );
    }

    void OnLoginSuccess(LoginResult result)
    {
        DisplayStatus("Login successful! Welcome back!", false);
        Debug.Log("Login OK — " + result.PlayFabId);

        SceneManager.LoadScene("Test_Lam"); // load scene bạn muốn
    }

    void OnLoginFailure(PlayFabError error)
    {
        DisplayStatus("Login failed: " + error.ErrorMessage, true);
        Debug.LogError("Login FAIL:\n" + error.GenerateErrorReport());
    }

    private void DisplayStatus(string status,bool isError)
    {
        if (!statusText.IsActive())
            statusText.gameObject.SetActive(true);
        if (isError)
            statusText.color = Color.red;
        else
            statusText.color = Color.green;
        statusText.text = status;
    }    
}
