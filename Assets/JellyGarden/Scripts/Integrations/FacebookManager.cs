using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

#if FACEBOOK
using Facebook.Unity;
#endif


public class FacebookManager : MonoBehaviour {
	private bool LoginEnable;
	public GameObject facebookButton;
	//1.3.3
	private string lastResponse = string.Empty;
	public static string userID;
	public static List<FriendData> Friends = new List<FriendData> ();

	protected string LastResponse {
		get {
			return this.lastResponse;
		}

		set {
			this.lastResponse = value;
		}
	}

	private string status = "Ready";

	protected string Status {
		get {
			return this.status;
		}

		set {
			this.status = value;
		}
	}

	bool loginForSharing;
	public static FacebookManager THIS;

	void Awake () {
		THIS = this;
	}

	void OnEnable () {
#if PLAYFAB
		NetworkManager.OnLoginEvent += GetUserName;

#endif
	}


	void OnDisable () {
#if PLAYFAB
		NetworkManager.OnLoginEvent -= GetUserName;

#endif
	}
	#if PLAYFAB || GAMESPARKS
	public FriendData GetCurrentUserAsFriend () {
		FriendData friend = new FriendData () {
			FacebookID = NetworkManager.facebookUserID,
			userID = NetworkManager.UserID,
			picture = InitScript.profilePic
		};
//		print ("playefab id: " + friend.PlayFabID);
		return friend;
	}
	#endif

	#region FaceBook_stuff

	#if FACEBOOK
	public void CallFBInit () {
		Debug.Log ("init facebook");
		FB.Init (OnInitComplete, OnHideUnity);

	}

	private void OnInitComplete () {
		Debug.Log ("FB.Init completed: Is user logged in? " + FB.IsLoggedIn);
		if (FB.IsLoggedIn) {//1.3
			CallFBLogin ();
		}

	}

	private void OnHideUnity (bool isGameShown) {
		Debug.Log ("Is game showing? " + isGameShown);
	}

	void OnGUI () {
		if (LoginEnable) {
			CallFBLogin ();
			LoginEnable = false;
		}

	}


	public void CallFBLogin () {
		Debug.Log ("login");
		FB.LogInWithReadPermissions (new List<string> () { "public_profile", "email", "user_friends" }, this.HandleResult);
	}

	public void CallFBLoginForPublish () {
		// It is generally good behavior to split asking for read and publish
		// permissions rather than ask for them all at once.
		//
		// In your own game, consider postponing this call until the moment
		// you actually need it.
		FB.LogInWithPublishPermissions (new List<string> () { "publish_actions" }, this.HandleResult);
	}

	public void CallFBLogout () { 
		FB.LogOut ();
		facebookButton.SetActive (true);
		#if PLAYFAB || GAMESPARKS
		NetworkManager.THIS.IsLoggedIn = false;
		#endif
		SceneManager.LoadScene ("game");
	}

	public void Share () {
		if (!FB.IsLoggedIn) {
			loginForSharing = true;
			LoginEnable = true;
			Debug.Log ("not logged, logging");
		} else {
			FB.FeedShare (
				link: new Uri ("http://apps.facebook.com/" + FB.AppId + "/?challenge_brag=" + (FB.IsLoggedIn ? AccessToken.CurrentAccessToken.UserId : "guest")),
//				linkName: FacebookSettings.AppLabels [0],
				linkCaption: "I just scored " + LevelManager.Score + " points! Try to beat me!"
				            //picture: "https://fbexternal-a.akamaihd.net/safe_image.php?d=AQCzlvjob906zmGv&w=128&h=128&url=https%3A%2F%2Ffbcdn-photos-h-a.akamaihd.net%2Fhphotos-ak-xtp1%2Ft39.2081-0%2F11891368_513258735497916_1832270581_n.png&cfs=1"
			);
		}
	}

	protected void HandleResult (IResult result) {
		if (result == null) {
			this.LastResponse = "Null Response\n";
			Debug.Log (this.LastResponse);
			return;
		}

		//     this.LastResponseTexture = null;

		// Some platforms return the empty string instead of null.
		if (!string.IsNullOrEmpty (result.Error)) {
			this.Status = "Error - Check log for details";
			this.LastResponse = "Error Response:\n" + result.Error;
			Debug.Log (result.Error);
		} else if (result.Cancelled) {
			this.Status = "Cancelled - Check log for details";
			this.LastResponse = "Cancelled Response:\n" + result.RawResult;
			Debug.Log (result.RawResult);
		} else if (!string.IsNullOrEmpty (result.RawResult)) {
			this.Status = "Success - Check log for details";
			this.LastResponse = "Success Response:\n" + result.RawResult;
			LoggedSuccefull ();//1.3
		} else {
			this.LastResponse = "Empty Response\n";
			Debug.Log (this.LastResponse);
		}
	}

	void LoggedSuccefull () {//1.3
		PlayerPrefs.SetInt ("Facebook_Logged", 1);
		PlayerPrefs.Save ();

		facebookButton.SetActive (false);//1.3.3

		//Debug.Log(result.RawResult);
		userID = AccessToken.CurrentAccessToken.UserId;
		GetPicture (AccessToken.CurrentAccessToken.UserId);
		
		#if PLAYFAB || GAMESPARKS
		NetworkManager.facebookUserID = AccessToken.CurrentAccessToken.UserId;
		NetworkManager.THIS.LoginWithFB (AccessToken.CurrentAccessToken.TokenString);
		#endif
	}

	void GetUserName () {
		FB.API ("/me?fields=first_name", HttpMethod.GET, GettingNameCallback);
	}

	private void GettingNameCallback (IGraphResult result) {
		if (string.IsNullOrEmpty (result.Error)) {
			IDictionary dict = result.ResultDictionary as IDictionary;
			string fbname = dict ["first_name"].ToString ();

			#if PLAYFAB || GAMESPARKS
			NetworkManager.THIS.UpdateName (fbname);
#endif
	
		}

	}


	void GetPicture (string id) {
		FB.API ("/" + id + "/picture", HttpMethod.GET, this.ProfilePhotoCallback);
	}

	private void ProfilePhotoCallback (IGraphResult result) {
		if (string.IsNullOrEmpty (result.Error) && result.Texture != null) {
			Sprite sprite = new Sprite ();
			sprite = Sprite.Create (result.Texture, new Rect (0, 0, 50, 50), new Vector2 (0, 0), 1f);
			InitScript.profilePic = sprite;

			#if PLAYFAB || GAMESPARKS
			NetworkManager.PlayerPictureLoaded ();

#endif
		}

	}



	public void SaveScores () {
		int score = 10000;

		var scoreData =
			new Dictionary<string, string> () { { "score", score.ToString () } };

		IDictionary<string, string> dic =
			new Dictionary<string, string> ();
		//dic.Add("stat_type", "http://samples.ogp.me/768772476466403");
		//dic.Add("object1", "{\"fb:app_id\":\"1040909022611487\",\"og:type\":\"object\",\"og:title\":\"111\"}");
		//FB.API("/me/scores", HttpMethod.POST, APICallBack, scoreData);
		//FB.API("me/objects/object1", HttpMethod.POST, APICallBack, dic);
		//"object": "{\"fb:app_id\":\"302184056577324\",\"og:type\":\"object\",\"og:url\":\"Put your own URL to the object here\",\"og:title\":\"Sample Object\",\"og:image\":\"https:\\\/\\\/s-static.ak.fbcdn.net\\\/images\\\/devsite\\\/attachment_blank.png\"}"

	}

	public void ReadScores () {

		FB.API ("/me/objects/object", HttpMethod.GET, APICallBack);
	}

	public void GetFriendsPicture () {
		FB.API ("me/friends?fields=picture", HttpMethod.GET, RequestFriendsCallback);
	}


	private void RequestFriendsCallback (IGraphResult result) {
		if (!string.IsNullOrEmpty (result.RawResult)) {
//			Debug.Log (result.RawResult);

			var resultDictionary = result.ResultDictionary;
			if (resultDictionary.ContainsKey ("data")) {
				var dataArray = (List<object>)resultDictionary ["data"];

				if (dataArray.Count > 0) {
					for (int i = 0; i < dataArray.Count; i++) {
						string friendID = "";
						var firstGroup = (Dictionary<string, object>)dataArray [i];
						//foreach (KeyValuePair<string, object> entry in firstGroup) {
						//    if (entry.Key == "id")
						friendID = "" + firstGroup ["id"];
						//if (entry.Key == "picture") {
						var pictures = (Dictionary<string, object>)firstGroup ["picture"];
						foreach (KeyValuePair<string, object> pictureUrl in pictures) {
							var pictures1 = (Dictionary<string, object>)pictureUrl.Value;
							foreach (KeyValuePair<string, object> pictureUrl1 in pictures1) {
								if (pictureUrl1.Key == "url") {
									FriendData friend = Friends.Find (delegate (FriendData bk) {
										return bk.FacebookID == friendID;
									}
									                    );
									if (friend != null)
										GetPictureByURL ("" + pictureUrl1.Value, friend);
								}
							}

							//}
						}
						//print(firstGroup["id"] + " " + firstGroup["title"]);
					}
					//this.gamerGroupCurrentGroup = (string)firstGroup["id"];
				}
			}
		

			if (!string.IsNullOrEmpty (result.Error)) {
				Debug.Log (result.Error);

			}
		}
	}

	public void GetPictureByURL (string url, FriendData friend) {
		StartCoroutine (GetPictureCor (url, friend));
	}

	IEnumerator GetPictureCor (string url, FriendData friend) {

		Sprite sprite = new Sprite ();
		WWW www = new WWW (url);
		yield return www;
		sprite = Sprite.Create (www.texture, new Rect (0, 0, 50, 50), new Vector2 (0, 0), 1f);
		friend.picture = sprite;
//		print ("get picture for " + url);
	}

	public void APICallBack (IGraphResult result) {
		Debug.Log (result);
	}

	#endif
	#endregion

}

public class FriendData {
	public string userID;
	public string FacebookID;
	public Sprite picture;
	public int level;
	public GameObject avatar;
}
