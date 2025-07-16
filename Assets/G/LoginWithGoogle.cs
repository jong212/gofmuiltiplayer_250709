using System.Collections;
using System.Collections.Generic;
using Firebase.Extensions;
using Google;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using UnityEngine.UI;
using UnityEngine.Networking;
using Firebase.Firestore;
using UnityEngine.SceneManagement;

/*

- 공통 로직
- 기능 : 인증, 인증 후 초기 유저데이터 서버 저장

===============================
*/

public class LoginWithGoogle : MonoBehaviour
{
    [Header("인증")]
    private GoogleSignInConfiguration configuration;
    private bool isGoogleSignInInitialized = false;
    public string GoogleAPI = "762595478734-t9o1s0t304a7oeaucp01b1j3ruo4m4bt.apps.googleusercontent.com";

    Firebase.Auth.FirebaseAuth auth;
    Firebase.Auth.FirebaseUser user;

    private FirebaseFirestore db;

    [Header("UI")]
    public Image UserProfilePic;
    private string imageUrl;
    public TextMeshProUGUI Username, UserEmail;

    [Header("fakeinfo")]
    string fakeUserId = "9RNjWgP28Admorqn1GVa6lbOgYG2";
    string fakeEmail = "jonghwa0212@gmail.com";
    string fakeName = "안종화";
    int c;
    public void Init()
    {
         InitFirebase();
    }

    // Firebase 초기화 (인증 + Firestore 인스턴스 할당)
    void InitFirebase()
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    // 구글 로그인 버튼 클릭 시 실행되는 함수
    // 1. 구글 로그인 시작
    // 2. 로그인 성공 시 Firebase Authentication 연동
    // 3. 기존/신규 관계없이 로그인 처리
    // 4. 로그인 성공 후 Firestore에 사용자 정보 있는지 확인 (CheckUser 호출)
    public void Login()
    {
        CheckFakeUser(); // PC에서 작업할땐 이 함수 사용하기 mobile에선 이 함수랑 아래 리턴문제거
        return;
        if (!isGoogleSignInInitialized)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                WebClientId = GoogleAPI,
                RequestEmail = true
            };

            isGoogleSignInInitialized = true;
        }
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            WebClientId = GoogleAPI
        };
        GoogleSignIn.Configuration.RequestEmail = true;

        Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();

        TaskCompletionSource<FirebaseUser> signInCompleted = new TaskCompletionSource<FirebaseUser>();
        signIn.ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                signInCompleted.SetCanceled();
                Debug.Log("Cancelled");
            }
            else if (task.IsFaulted)
            {
                signInCompleted.SetException(task.Exception);

                Debug.Log("Faulted " + task.Exception);
            }
            else
            {
                // 구글 게정 선택하는 순간 아래의 else가 실행 되면서 파이어베이스에서 기존 회원인지 신규인지를 판단하고 신규인 경우에만 Authentication 에 등록이 된다
                Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(((Task<GoogleSignInUser>)task).Result.IdToken, null);
                auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
                {
                    if (authTask.IsCanceled)
                    {
                        signInCompleted.SetCanceled();
                    }
                    else if (authTask.IsFaulted)
                    {
                        signInCompleted.SetException(authTask.Exception);
                        Debug.Log("Faulted In Auth " + task.Exception);
                    }
                    else
                    {
                        signInCompleted.SetResult(((Task<FirebaseUser>)authTask).Result);
                        Debug.Log("Success");
                        user = auth.CurrentUser;
                        Username.text = user.DisplayName;
                        UserEmail.text = user.Email;

                        // 이부분은 샘플이라 나중에 필요없을떄 빼던가 하면 됨 TO DO
                        StartCoroutine(LoadImage(CheckImageUrl(user.PhotoUrl.ToString())));
                        CheckUser();
                    }
                });
            }
        });
    }
     private string CheckImageUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            return url;
        }
        return imageUrl;
    }
     IEnumerator LoadImage(string imageUri)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUri);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            Debug.Log("Image loaded successfully");
            UserProfilePic.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
        }
        else
        {
            Debug.Log("Error loading image: " + www.error);
        }
    }
    // 로그인 성공 후 Firestore에서 사용자 문서 존재 여부 확인
    // - 존재하면 아무 것도 하지 않음 (기존 유저)
    // - 존재하지 않으면 SetAsync로 초기 유저 데이터 저장 (신규 유저 등록)
    async void CheckUser()
    {
        var userDoc = db.Collection("users").Document(user.UserId);

        // MEMO
        var snap = await userDoc.GetSnapshotAsync();          

        // 데이터가 없네? 추가 ㄱ
        if (!snap.Exists)                                     
        {
            await userDoc.SetAsync(new Dictionary<string, object> {
            { "email",       user.Email       },
            { "displayName", user.DisplayName },
            { "coins",       0                },
            { "level",       1                }
        });                                               //   저장까지 대기
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    private void Update()
    {
        if(c != null) { Debug.Log(c); }
    }

    // 아래 다 테스트 코드임 (PC용)
    public void OnClickTestUpdate()
    {
        _ = TestUpdateAsync(); // db의 값 에서 증가 되는지 체크하는 테스트 함수
    }

    public async Task TestUpdateAsync()
    {
        var userDoc = db.Collection("users").Document(user.UserId);

        var snapshot = await userDoc.GetSnapshotAsync();
        if (snapshot.Exists)
        {
            await userDoc.UpdateAsync("coins", FieldValue.Increment(10)); // 코인 +10
        }
    }

    // 아래 다 테스트 코드임 (PC용)
    public async void CheckFakeUser()
    {
        db = FirebaseFirestore.DefaultInstance; // 이거 Init 안 돼 있으면 null일 수도 있음

        string uid = fakeUserId;
        string email = fakeEmail;
        string displayName = fakeName;

        var userDoc = db.Collection("users").Document(uid);

        var snap = await userDoc.GetSnapshotAsync();

        if (!snap.Exists)
        {
            await userDoc.SetAsync(new Dictionary<string, object> {
            { "email", email },
            { "displayName", displayName },
            { "coins", 0 },
            { "level", 1 }
        });

            Debug.Log("🔥 Fake 유저 데이터 새로 생성됨");
        }
        else
        {
            Debug.Log("✅ 기존 Fake 유저 데이터 존재함");
             c = snap.GetValue<int>("coins");
            Debug.Log(c);
        }
        StartCoroutine(AddressableMng.instance.InitializeAllPrefabs(
            new List<string> { "Test" },
            () => {
                Debug.Log("모든 어드레서블 프리팹 캐싱 완료!");

                StartCoroutine(LoadSceneAsync("Preloader"));
            }
        ));        
    }
    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;  // 씬 로드가 완료될 때까지 대기
        }
    }
}