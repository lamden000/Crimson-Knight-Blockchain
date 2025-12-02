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

        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = userField.text 
        };

        PlayFabClientAPI.UpdateUserTitleDisplayName(request,
            res => {
                Debug.Log("DisplayName set OK!");
                // Chuyển sang scene login hoặc tự login luôn
            },
            err => Debug.LogError(err.GenerateErrorReport())
        );
    }

    void OnRegisterFailure(PlayFabError error)
    {
        DisplayStatus("Register failed: " + error.ErrorMessage, true);
        Debug.LogError("Register FAIL:\n" + error.GenerateErrorReport());
    }

    public void OnPressLogin()
    {
        if (userField.gameObject.activeSelf)
        {
            // Ẩn username field nếu đang hiện (vì login không cần)
            userField.gameObject.SetActive(false);
            usernameRequired = false;

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
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetUserAccountInfo = true
            }
        };

        PlayFabClientAPI.LoginWithEmailAddress(
            request,
            OnLoginSuccess,
            OnLoginFailure
        );
    }


    void OnLoginSuccess(LoginResult result)
    {
        var data = new PlayerData();

        data.userId = result.PlayFabId;

        // username bạn đã đăng ký
        string username = result.InfoResultPayload.AccountInfo.Username;
        data.username = username;

        // dữ liệu còn lại tuỳ bạn
        data.level = 1;
        data.exp = 0;
        data.gold = 0;

        PlayerDataManager.Instance.Initialize(data);

        SceneManager.LoadScene("Test_Lam");
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
