using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System.Collections.Generic;
using LitJson;
using System.IO.Compression;
using System;
using Vuforia;


public class TakePicture : MonoBehaviour 
{
	//*****THE getData.php FILE THAT YOU NEED TO HOST ON A SERVER THAT HAS NODE.JS INSTALLED IS IN THE ASSETS FOLDER!*****

	//change this to reflect your custom hosted url followed by getData.php?url=
	private const string BASE_URL = "www.cshansraj.in/getData.php?url=";
	//enter your google api key for custom search here
	private const string GOOGLE_API_KEY = "AIzaSyBWe4um68TiqKSxkhUAAr9DpTUGpoaGgo0";
	//enter your cloud name from cloudinary here
	private const string CLOUD_NAME = "dhtuzvnsm";
	//enter your cloudinary upload preset name
	private const string UPLOAD_PRESET_NAME = "sx0mwjid";


	//Google Search Engine ID: 005774657766531524867:cxdw1v8dd48


	//private const string CLOUDINARY_API_KEY = "464228211727792";

	//private const string CLOUDINARY_SIGNATURE = "LUubXRT37rDkg8hI__AjyOEubWY";

	private const string IMAGE_SEARCH_URL = "https://www.google.com/searchbyimage?site=search&sa=X&image_url=";

	//private const string GOOGLE_SEARCH_URL = "https://www.googleapis.com/customsearch/v1?key=" + GOOGLE_API_KEY +"&cref&q=";

	string GOOGLE_SEARCH_URL1= "https://www.googleapis.com/customsearch/v1?q=";
	string GOOGLE_SEARCH_URL2 = "&searchType=image&cx=005774657766531524867:cxdw1v8dd48&key="+ GOOGLE_API_KEY;
	byte[] imageByteArray;

	public GameObject buttonObject;

	private string imageURl;

	private string imageIdentifier;

	private string timeStamp;

	private string wordsToSearch;

	private GameObject scanningObject;

	private GameObject line1Object;

	private GameObject line2Object;

	private Vector3 cameraForwardVector;
	private Vector3 cameraPosition;

	private GameObject debug;

	void Start(){

		buttonObject = GameObject.Find ("Button");
		scanningObject = GameObject.Find ("Image");
		line1Object = GameObject.Find ("line1");
		line2Object = GameObject.Find ("line2");

		debug = GameObject.Find("debug");

		//set default states of UI 
		scanningObject.SetActive (false);
		line1Object.SetActive (false);
		line1Object.transform.parent.gameObject.SetActive (false);
		line2Object.SetActive (false);
		line2Object.transform.parent.gameObject.SetActive (false);
		debug.SetActive(false);




		VuforiaARController.Instance.RegisterVuforiaStartedCallback(OnVuforiaStarted);
		VuforiaARController.Instance.RegisterOnPauseCallback(OnPaused);



	}

	private void OnVuforiaStarted() {
	    CameraDevice.Instance.SetFocusMode(
	    CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
	}

	private void OnPaused(bool paused)
{
    if (!paused) // resumed
    {
        // Set again autofocus mode when app is resumed
        CameraDevice.Instance.SetFocusMode(
            CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
    }
}

	/*this takes a screenshot, saves it to file, and reads the file to the variable imageByteArray
	 * I tried other methods of taking a picture like using Vuforia's Image class and also creating another
	 * Unity webcamtexture, none of which worked on both mobile and in the editor so this is what I landed on
	 */ 
	public IEnumerator TakePhoto()
	{
		string filePath;

			//on mobile platforms persistentDataPath is already prepended to file name when using CaptureScreenshot()
			if (Application.isMobilePlatform) {

				filePath = Application.persistentDataPath + "/image.png";
				ScreenCapture.CaptureScreenshot ("/image.png");
				//must delay here so picture has time to save unfortunatly
				yield return new WaitForSeconds(1.5f);
				//Encode to a PNG
				imageByteArray = File.ReadAllBytes(filePath);

			} else {

				filePath = Application.dataPath + "/StreamingAssets/" + "image.png";
				ScreenCapture.CaptureScreenshot (filePath);
				//must delay here so picture has time to save unfortunatly
				yield return new WaitForSeconds(1.5f);
				//Encode to a PNG
				imageByteArray = File.ReadAllBytes(filePath);
			}

		print ("photo done!!");
		Debug.Log("photo done!!");
		debug.GetComponent<Text> ().text = "photo done!!";
		Debug.Log(imageByteArray+"    "+imageByteArray.Length);
		debug.GetComponent<Text> ().text = ""+imageByteArray.Length;


		StartCoroutine("UploadImage");

		buttonObject.SetActive (false);
		scanningObject.SetActive (true);

	}

	public static byte[] Compress(byte[] data)
		{
		    MemoryStream output = new MemoryStream();
		using (DeflateStream dstream = new DeflateStream(output,  CompressionMode.Compress))
		    {
		        dstream.Write(data, 0, data.Length);
		    }
		    return output.ToArray();
		}

		/*public static byte[] Decompress(byte[] data)
		{
		    MemoryStream input = new MemoryStream(data);
		    MemoryStream output = new MemoryStream();
		    using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
		    {
		        dstream.CopyTo(output);
		    }
		    return output.ToArray();
		}*/


	//uploads the image to Cloudinary (you must first create an unsigned upload preset for this to work) and gets the image url
	public IEnumerator UploadImage(){

		debug.GetComponent<Text> ().text = "uploading";


		Debug.Log ("uploading image...");
		string url = "https://api.cloudinary.com/v1_1/" + CLOUD_NAME + "/auto/upload/";
		WWWForm myForm = new WWWForm ();
		myForm.AddBinaryData ("file",imageByteArray);


		myForm.AddField ("upload_preset", UPLOAD_PRESET_NAME);

		WWW www = new WWW(url,myForm);
		yield return www;
		Debug.Log (www.text);

		Debug.Log ("done uploading!");
		debug.GetComponent<Text> ().text = "done uploading";


		//parse resulting string to get image url 
		//imageURl = www.text.Split('"', '"')[42];
		string[] cloudURL = www.text.Split('"', '"');

		Debug.Log("ClouseURL:"+cloudURL[43]);

		imageURl = cloudURL[43];

		/*I got burned out trying to figure out how to delete an image after we use it
		 * so if someone else could figure it out that would be great, you will probably
		 * need this image identifier and timestamp.
		 * imageIdentifier = www.text.Split('"', '"')[3]; 
		 * timeStamp = www.text.Split('"', '"')[25]; 
		 * print ("IMAGE Identifier: " + imageIdentifier);
		 * print ("TIMESTAMP: " + timeStamp);
		*/

		StartCoroutine (reverseImageSearch());

	}

	/*this function passes the image url to the php script called getData.php that you must have hosted somewhere that node.js is also installed.
	 * The php will parse the line from the Google reverse image search that contains the best guess as to what the image is.
	 * This was the only way I could figure to follow the redirects from Google and also be able to see the HTML rendered from the Javascript.
	 * We can then take that line and parse it down further to get our wordsToSearch variable. 
	 */ 
	public IEnumerator reverseImageSearch(){

		//create the full search url by adding all 3 together
		string fullSearchURL = BASE_URL + WWW.EscapeURL(IMAGE_SEARCH_URL + imageURl);
		print (fullSearchURL);

		//create a new www object and pass in this search url
		WWW www = new WWW(fullSearchURL);
		yield return www;


		wordsToSearch = www.text.Substring(www.text.IndexOf(">")+1);

		Debug.Log (wordsToSearch);	
		debug.GetComponent<Text> ().text = wordsToSearch;


		StartCoroutine ("GoogleSearchAPI");

	}

	//This does a custom google search for the wordsToSearch (google's best guess) 
	public IEnumerator GoogleSearchAPI ()
	{

		//string searchURL = GOOGLE_SEARCH_URL1 + WWW.EscapeURL (wordsToSearch) + GOOGLE_SEARCH_URL2;
		//send a new request to the google custom search API
		string searchURL = "https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro=&explaintext=&titles=" + wordsToSearch;

		WWW www = new WWW (searchURL);
		yield return www;

		//THIS IS PROBABLY A BETTER JOB FOR WITH REGEX-but lets parse it like this so I don't have to explain regex or deserializing JSON in my video.

		//split string by lines
		//var parsedData = www.text.Split('\n');

		Debug.Log ("Wiki//: " + www.text);

		debug.GetComponent<Text> ().text = www.text;


		JsonData parsedData = JsonMapper.ToObject (www.text);


		try {
			Debug.Log (parsedData ["query"] ["pages"] [0] ["extract"].ToString ());

			string info = parsedData ["query"] ["pages"] [0] ["extract"].ToString ();

			debug.GetComponent<Text> ().text = info;
			CreateVisibleText (wordsToSearch, info);


		} catch (Exception e) {
			string info = "Nothing Found!!";
			debug.GetComponent<Text> ().text = info;

			CreateVisibleText (wordsToSearch, info);

		}






		//set default lines for the first result on google
//		if (parsedData.Length > 42) {
//			string line1 = parsedData [43];
//			string line2 = parsedData [47];
//
//			//print("line1");
//			//print("line2");
//
//			//lets check for wikipedia results and if there are any we will overwrite our default values
//			for (int i = 0; i < parsedData.Length; i++) {
//
//				if (parsedData [i].Contains ("Wikipedia")) {
//					line1 = parsedData [i];
//					line2 = parsedData [i + 4];
//					break;
//				}
//			}
//
//			//remove first unwanted characters from string
//			line1 = line1.Remove(0,12);
//			line2 = line2.Remove (0, 12);
//			//remove last unwanted characters from string
//			line1 = line1.Remove (line1.Length - 2);
//			line2 = line2.Remove (line2.Length - 2);
//
//			//remove new line characters from string we will add our own later.
//			if (line2.Contains("\n")){
//				line2.Replace("\n"," ");
//			}




	
		scanningObject.SetActive (false);
		buttonObject.SetActive (true);

	}

	public void CreateVisibleText (string head, string info)
	{


		

		//turn on both 3d text objects
		line1Object.SetActive (true);
		line2Object.SetActive (true);
		line1Object.transform.parent.gameObject.SetActive (true);
		line2Object.transform.parent.gameObject.SetActive (true);
		debug.SetActive(false);

		head = head.ToUpper ();

		//replace the spaces in the best guess result from google with new lines (if there are any)
		line1Object.GetComponent<Text> ().text = head;

		//remove new line characters from text3
		if (info.Contains ("\\n")) {
			info = info.Replace (@"\n", " ");
		}


		string firstLine;

		if (info.Contains (".")) {
			int firstLineI = info.IndexOf ('.');
			firstLine = info.Substring (0, firstLineI);
		} else {
			firstLine = info;
		}


		//loop through all characters of the text and insert a new line after every third space.
		int spaceCounter = 0;
		for (int i = 0; i < firstLine.Length; i++) {

			if (firstLine[i] == ' '){
				spaceCounter++;
				if (spaceCounter % 7 == 0) {

					firstLine = firstLine.Insert (i, "\n");
				}
			}
		}

		print("text1"+head);
		print("text2"+firstLine);

		//I decided to not display the title of the webpage (text2) but you can add it here if you like. 
		line2Object.GetComponent<Text> ().text = firstLine;

	}

	//this is the function that gets called when the scan button is pressed.
	public void StartCamera(){
		//turn off text and backgrounds
		line1Object.SetActive (false);
		line2Object.SetActive (false);
		line1Object.transform.parent.gameObject.SetActive (false);
		line2Object.transform.parent.gameObject.SetActive (false);
		debug.SetActive(true);

//
		print ("button down....");

		//starts the process
		StartCoroutine ("TakePhoto");
	}
}