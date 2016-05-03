using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;




public class CarController : MonoBehaviour {

	// TTL for all event messages
	private static readonly int BASE_TTL = 5;

	// Size of a grid square
	private static readonly float GRID_ELEMENT_SIZE = 100F;

	// Time between frames
	private static readonly float FRAME_INTERVAL = 0.2F;

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
	private static readonly float minLat = 40.981F;
	private static readonly float maxLat = 41.399F;
	private static readonly float minLon = -8.795F;
	private static readonly float maxLon = -8.358F;

	// Center of the map
	static readonly Vector3 center=   new Vector3(41F, -8.6F,0.0F);

	// Image Proportions
	public static float imWidth;
	public static float imHeight;

	// The car dot renderer
	private SpriteRenderer rend;

	// Variable indicating if the car should react to an external event
	public bool react;



	// Start on Demand
	public void Begin (String filename) {
		react = false;

		//TODO: Review this port business
		int port = 8000 + int.Parse (filename.Split ('.') [1]);
		File.WriteAllText (Application.dataPath+"/port.txt", port.ToString());

		//detectionSocket = GetSocket(detectionPort)
		//communicationSocket = GetSocket (communicationPort);

		rend = GetComponent<SpriteRenderer>();

		FileInfo f = new FileInfo (filename);
		reader = f.OpenText();

		text = "start";

		transform.position = ConvertCoords (center.x, center.y);

		// Use this to update in custom intervals, create UpdatePosition for that
		InvokeRepeating("UpdatePosition", 0, FRAME_INTERVAL);

		//SpawnPython ();

		// Receive external events
		//communicationSocket.BeginReceive (receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback),null );

	}

	// Handler for external event receptions
	private void ReceiveCallback(IAsyncResult result){
		int receivedLength = communicationSocket.EndReceive (result);

		byte[] sizedData = new byte[receivedLength];
		Buffer.BlockCopy (receiveBuffer, 0, sizedData, 0, receivedLength);

		//int ttl = BitConverter.ToInt32 (sizedData,0);

		//TODO: If received id is my id, discard, otherwise, set react to true
		if(/*content*/)
			react = true;

		//Continue receiving (events may be lost)
		communicationSocket.BeginReceive (receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback),null );
	}
		

	// Spawns a python instance
	private void SpawnPython(){
		ProcessStartInfo psi = new ProcessStartInfo(); 
		psi.FileName = "/bin/sh";
		psi.UseShellExecute = false; 
		psi.RedirectStandardOutput = true;
		psi.Arguments = "runFlareCast.sh";

		Process p = Process.Start(psi); 
		string strOutput = p.StandardOutput.ReadToEnd(); 
		p.WaitForExit(); 
		UnityEngine.Debug.Log(strOutput);
	}

	//Called every 0.2 seconds
	public void UpdatePosition () {

		// React to external event
		if (react) {
			rend.color = new Color (0f, 0f, 1f);
			react = false;
			BroadcastNearby (/*possible args*/);
		}


		text = reader.ReadLine();
		if (text != null && text != "NOP") {
			string[] tokens = text.Split (' ');

			double coordX = double.Parse (tokens [0], CultureInfo.InvariantCulture.NumberFormat);
			double coordY = double.Parse (tokens [1], CultureInfo.InvariantCulture.NumberFormat);

			transform.position = ConvertCoords (coordX, coordY);


			if(int.Parse (tokens [2]) == 1) {
				rend.color = new Color (1f, 0f, 0f);
				detectionSocket.Send (/*content*/);
			}
			else
				rend.color = new Color (0f, 1f, 0f);
		}
	}

	private void BroadcastNearby(/*possible args*/){
		for (int i = 0; i < CarManager.NR_CARS; i++) {
			CarController car = CarManager.cars [i];
			bool sameGridX = Math.Floor (car.transform.position.x / GRID_ELEMENT_SIZE) == Math.Floor (gameObject.transform.position.x / GRID_ELEMENT_SIZE);
			bool sameGridY = Math.Floor (car.transform.position.y / GRID_ELEMENT_SIZE) == Math.Floor (gameObject.transform.position.y / GRID_ELEMENT_SIZE);
			if (sameGridX && sameGridY)
				car.communicationSocket.Send (/*content*/);
		}
	}

	private Vector3 ConvertCoords (double lat, double lon){
		double proportionX = (lon - minLon) / (maxLon - minLon);
		double proportionY = (lat - minLat) / (maxLat - minLat);

		double x = (proportionX * imWidth) - (imWidth / 2);
		double y = (proportionY * imHeight) - (imHeight / 2);

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

	}

}
