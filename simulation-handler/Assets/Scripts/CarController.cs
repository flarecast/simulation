using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Collections.Generic;




public class CarController : MonoBehaviour {


	// TTL for all event messages
	private static readonly int LIFETIME = 10;

	// The port from which the program counts up
	private static readonly int BASE_PORT = 9100;

	//Speed of interpolated movement
	private static readonly int SPEED = 200;

	// Time between frames
	private static readonly float FRAME_INTERVAL = 0.1F;

	// Distance multiplier for the radius
	public static readonly float RADIUS_SCALE = 5;

	private static readonly String pythonAddress = "127.0.0.1";

	// Events detected in the file are sent to the python detector through this socket
	private Socket detectionSocket;

	// Events from other threads are received through this socket.
	// Events are forwarded through this socket
	private Socket communicationSocket;

	private byte[] receiveBuffer = new byte[1024];

	// Variable for a positions file line
	private string text;
	// Positions file reader
	private StreamReader reader;


	// Map coordinate limits
	private static readonly double minLat = 40.980779F;
	private static readonly double maxLat = 41.399837F;
	private static readonly double minLon = -8.795317F;
	private static readonly double maxLon = -8.358211F;

	// Center of the map
	static readonly Vector3 center=   new Vector3(41.190308F, -8.576764F,0.0F);

	// Image Proportions
	public static float imWidth;
	public static float imHeight;

	// The car dot renderer
	private SpriteRenderer rend;

	// Variable containing the next message to broadcast
	public Queue<byte[]> messages;

	//Next file position of the current car
	private Vector3 nextPosition;

	//If the file should react to an event received from another entity
	public bool react;

	// Start on Demand
	public void Begin (String filename, int carNr) {

		//Create radius around the dot
		GameObject radius = Instantiate (Resources.Load("Radius")) as GameObject;
		radius.transform.position = gameObject.transform.position;
		radius.transform.parent = gameObject.transform;
		radius.transform.localScale = new Vector3(RADIUS_SCALE, RADIUS_SCALE, 0);
		radius.transform.position = new Vector3 (1, 1, 0);


		react = false;
		messages = new Queue<byte[]>();

		rend = GetComponent<SpriteRenderer>();

		FileInfo f = new FileInfo (filename);
		reader = f.OpenText();

		text = "start";

		// Use this to update in custom intervals, create UpdatePosition for that
		InvokeRepeating("UpdatePosition", 0, FRAME_INTERVAL);

		SpawnPython (BASE_PORT+carNr);

		detectionSocket = GetSocket (BASE_PORT+carNr);
		communicationSocket = GetSocket (BASE_PORT+carNr+1);

		// Receive external events
		communicationSocket.BeginReceive (receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback),null );

	}

	// Handler for external event receptions
	private void ReceiveCallback(IAsyncResult result){
		Socket rs = (Socket) result.AsyncState;

		//Code to copy data in correct size. See issue #2 in Github Repo
		/*byte[] sizedData = new byte[receivedLength];
		Buffer.BlockCopy (receiveBuffer, 0, sizedData, 0, receivedLength);*/

		messages.Enqueue (receiveBuffer);
		react = true;

		int receivedLength = rs.EndReceive (result);
		result.AsyncWaitHandle.Close();
		receiveBuffer = new byte[1024];

		//Continue receiving (events may be lost)
		rs.BeginReceive (receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback),null );
	}
		

	// Spawns a python instance
	private void SpawnPython(int port){
		ProcessStartInfo psi = new ProcessStartInfo(); 
		psi.FileName = "/bin/sh";
		psi.UseShellExecute = false; 
		psi.RedirectStandardOutput = true;
		psi.Arguments = "runFlareCast.sh "+port;

		Process p = Process.Start(psi); 

		//Give some time for the process to start, otherwise, the "Connect" won't be successful
		System.Threading.Thread.Sleep(500);
	}

	public void ApplyReaction(){
		rend.color = new Color (1f, 0f, 0f);
		if (react)
			react = false;
	}

	//Called every FRAME_INTERVAL seconds
	public void UpdatePosition () {

		BroadcastNearby ();

		if (react)
			ApplyReaction();

		text = reader.ReadLine();
		if (text != null && text != "NOP") {
			string[] tokens = text.Split (' ');

			double coordX = double.Parse (tokens [0], CultureInfo.InvariantCulture);
			double coordY = double.Parse (tokens [1], CultureInfo.InvariantCulture);

			nextPosition = ConvertCoords (coordX, coordY);

			if (int.Parse (tokens [2]) == 1) {
				ApplyReaction ();
				detectionSocket.Send (BitConverter.GetBytes(LIFETIME));
			}
			else
				rend.color = new Color (0f, 1f, 0f);
		}
	}

	private void BroadcastNearby(){
		for (int i = 0; i < CarManager.nr_cars; i++) {
			CarController car = CarManager.cars [i];
			float dist = Vector3.Distance(car.transform.position, transform.position);
			if (car.GetInstanceID() != gameObject.GetInstanceID() && dist*20 < RADIUS_SCALE * transform.localScale.x) {
				byte[] message;
				string result;
				while (messages.Count > 0) {
					message = messages.Dequeue ();

					// 2 lines for debug
					result = System.Text.Encoding.UTF8.GetString(message);
					UnityEngine.Debug.Log (result);

					car.communicationSocket.Send (message);
				}
				//debug react
				//car.react = true;
			}
		}
	}

	private Vector3 ConvertCoords (double lat, double lon){
		double proportionX = (lon - minLon) / (maxLon - minLon);
		double proportionY = (lat - minLat) / (maxLat - minLat);

		double x = (proportionX * imWidth) - (imWidth / 2);
		double y = (proportionY * imHeight) - (imHeight / 2);

		UnityEngine.Debug.Log (new Vector3 ((float)x, (float)y, 0.0F));

		return ( new Vector3((float) x , (float) y , 0.0F));
	}

	private Socket GetSocket(int port){
		IPEndPoint ipe = new IPEndPoint (IPAddress.Parse(pythonAddress), port);
		Socket tempSocket = 
			new Socket (ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


		tempSocket.Connect (ipe);

		if (tempSocket.Connected)
			return tempSocket;
		else
			return null;
	}

	private static byte[] StrToByteArray(string str)
	{
		Encoding encoding = Encoding.UTF8;
		return encoding.GetBytes(str);
	}



	// Use this for initialization (UNUSED, REPLACED BY Begin for On-Demand Calling)
	public void Start () {
	}


	// Update is called once per frame
	public void Update () {
		float step = SPEED * Time.deltaTime;
		//Interpolated (smooth)
		transform.position = Vector3.MoveTowards(transform.position, nextPosition, step);

		//Not Interpolated 
		//transform.position = nextPosition;
	}

}
