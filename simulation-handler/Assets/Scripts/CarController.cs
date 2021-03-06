﻿using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.UI;

public class CarController : MonoBehaviour {
	// TTL for all event messages (seconds)
	private static readonly float LIFETIME = 0.2F;

	// Has finished setup and is ready to read from file and socket
	public static bool working = false;

	// The port from which the program counts up
	private static readonly int BASE_PORT = 20000;

	//Speed of interpolated movement
	private static readonly int SPEED = 1000;

	// Time between frames (seconds)
	public static readonly float FRAME_INTERVAL = 0.1F;

	// Distance multiplier for the radius
	public static readonly float RADIUS_SCALE = 15;

	private float sizeFactor;

	private static readonly String pythonAddress = "127.0.0.1";

	// Events detected in the file are sent to the python detector through this socket
	public Socket detectionSocket;

	// Events from other threads are received through this socket.
	// Events are forwarded through this socket
	public Socket communicationSocket;

	// Variable for a positions file line
	private string text;

	private static int line;

	public string positionFile;

	// Positions file reader
	private StreamReader reader;

	// Use immediate message reaction or at intervals
	private static readonly bool immediateBroadcast = false;

	public static bool[] defaultToSend;

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
	public Vector3 nextPosition;

	//The number of this car
	public int carNumber;

	// Start on Demand
	public void Begin (String filename, int carNr) {

		carNumber = carNr;

		//Create radius around the dot
		GameObject radius = Instantiate (Resources.Load("Radius")) as GameObject;
		radius.transform.position = gameObject.transform.position;
		radius.transform.parent = gameObject.transform;
		radius.transform.localScale = new Vector3(RADIUS_SCALE, RADIUS_SCALE, 0);
		radius.transform.position = new Vector3 (1, 1, 0);

		sizeFactor = transform.localScale.x;
		messages = new Queue<byte[]>();
		rend = GetComponent<SpriteRenderer>();
		positionFile = filename;
		nextPosition = new Vector3 (5000, 5000, 100);

		FileInfo f = new FileInfo (filename);
		reader = f.OpenText();

		text = "start";

		// Use this to update in custom intervals, create UpdatePosition for that
		InvokeRepeating("UpdatePosition", 0, FRAME_INTERVAL);

	}

	// Spawns Flarecast, connects sockets for event transmission and starts listening
	public void SetupSockets(){
		int port = BASE_PORT + carNumber * 2;

		Thread t1 = new Thread (delegate() {
			detectionSocket = GetSocket (port);
		});

		t1.Start ();

		Thread t2 = new Thread (delegate() {
			communicationSocket = GetSocket (port + 1);
		});

		t2.Start ();

		SpawnPython (port);

		t2.Join ();
		t1.Join ();

		// DEBUG: Check if sockets are connected
		//UnityEngine.Debug.Log ("DETECTION  " + detectionSocket.Connected);
		//UnityEngine.Debug.Log ("COMM  " + communicationSocket.Connected);

	}

	// Connects socket to the python instance
	private Socket GetSocket(int port){
		IPEndPoint ipe = new IPEndPoint (IPAddress.Parse(pythonAddress), port);
		Socket tempSocket = 
			new Socket (ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

		tempSocket.Bind(ipe);
		tempSocket.Listen(10);

		return tempSocket.Accept();
	}
		
	// Spawns a python instance
	private void SpawnPython(int port){
		ProcessStartInfo psi = new ProcessStartInfo(); 
		psi.FileName = "/Users/joaorodrigues/.pyenv/versions/3.4.3/bin/python";
		psi.UseShellExecute = false;
		psi.RedirectStandardOutput = true;
		psi.RedirectStandardError = true;
		psi.EnvironmentVariables["FLARECAST_PORT"] = port.ToString();
		psi.Arguments = "flarecast-core/bin/app.py";
		UnityEngine.Debug.Log ("Started Python in port " + port + " and "+(port+1));

		Process p = new Process ();
		p.StartInfo = psi;
		p.OutputDataReceived += new DataReceivedEventHandler (PythonOutputHandler);
		p.ErrorDataReceived += new DataReceivedEventHandler (PythonErrorHandler);
		p.Start ();
		p.BeginOutputReadLine ();
		p.BeginErrorReadLine ();
	}


	// Start receiving messages in communication socket. Will be called recursively and asynchronously
	public void StartReceiving(){
		int size = 1024;
		byte[] buffer = new byte[size];
		communicationSocket.BeginReceive(buffer, 0, size, SocketFlags.None, (state) =>
			{
				int bytesReceived = communicationSocket.EndReceive(state);

				if(bytesReceived > 0){
					if(immediateBroadcast)
						BroadcastMessage(buffer, defaultToSend);
					else
						messages.Enqueue (buffer);
					UnityEngine.Debug.Log (carNumber + " RECEIVED ::::::::::::::::::::::::::::::::::::::::");
				}

				StartReceiving();
			} ,null);
	}

	// Handles Python output messages
	private static void PythonOutputHandler(object sendingProcess, 
		DataReceivedEventArgs outLine)
	{
		if (!String.IsNullOrEmpty(outLine.Data))
		{
			UnityEngine.Debug.Log (outLine.Data);
		}
	}

	// Handles Python error messages
	private static void PythonErrorHandler(object sendingProcess, 
		DataReceivedEventArgs errLine)
	{
		if (!String.IsNullOrEmpty(errLine.Data))
		{
			UnityEngine.Debug.Log (errLine.Data);
		}
	}


	//Called every FRAME_INTERVAL seconds
	public void UpdatePosition () {

		if (messages.Count != 0)
			rend.color = new Color (1f, 0f, 0f);
		else
			rend.color = new Color (0f, 1f, 0f);


		if(!immediateBroadcast)
			BroadcastNearby();


		// If all cars have been set up and connected to their python instances	
		if (working) {
			if (carNumber == 0) {
				GameObject.Find ("Status").GetComponent<Text>().text = line.ToString ();
				line++;
			}


			text = reader.ReadLine ();
			if (text != null && text != "nop") {
				string[] tokens = text.Split (' ');

				double coordX = double.Parse (tokens [0], CultureInfo.InvariantCulture);
				double coordY = double.Parse (tokens [1], CultureInfo.InvariantCulture);

				nextPosition = ConvertCoords (coordX, coordY);

				if (int.Parse (tokens [2]) == 1)
					detectionSocket.Send (BitConverter.GetBytes (LIFETIME));
			}
		}

		// DEBUG: Identify a car
		if(carNumber == 1000)
			rend.color = new Color (0f, 0f, 1f);
		
	}

	// Sends a given message to nearby cars
	private bool[] BroadcastMessage(byte[] message, bool[] to_send ){
		bool[] nextSend = new bool[CarManager.nr_cars];

		for (int i = 0; i < CarManager.nr_cars; i++) {
			CarController car = CarManager.cars [i];
			float dist = Vector3.Distance(car.nextPosition, nextPosition);

			// DEBUG: Distances when broadcasting
			//UnityEngine.Debug.Log("D: "+(dist)+" R: "+ (sizeFactor*(RADIUS_SCALE/10))*2);
			if (i!=carNumber && to_send[i] == true) {
				if (dist < (sizeFactor * (RADIUS_SCALE / 10)) * 2 * 0.9) {
					// DEBUG: Warn about one car sending to another
					//UnityEngine.Debug.Log ("Send from " + carNumber + " to " + i);
					car.communicationSocket.Send (message);
				} else
					nextSend [i] = true;
			}
		}
		return nextSend;
	}

	// Broadcasts the queued messages to nearby cars (except the current one)
	private void BroadcastNearby(){
		while (messages.Count > 0) {
			byte[] message = messages.Dequeue ();
			bool[] nextSend = BroadcastMessage (message, defaultToSend);
			new Thread (delegate() {
				for (int j = 0; j < LIFETIME; j += 1) {
					BroadcastMessage (message, nextSend);
					Thread.Sleep (1000);
				}
			}).Start ();
		}
	}

	// Converts GPS coordinates to image position
	private Vector3 ConvertCoords (double lat, double lon){
		double proportionX = (lon - minLon) / (maxLon - minLon);
		double proportionY = (lat - minLat) / (maxLat - minLat);
		double x = (proportionX * imWidth) - (imWidth / 2);
		double y = (proportionY * imHeight) - (imHeight / 2);

		// Print Coordinates for Debug
		//UnityEngine.Debug.Log (new Vector3 ((float)x, (float)y, 0.0F));

		return ( new Vector3((float) x , (float) y , 0.0F));
	}
		

	private static byte[] StrToByteArray(string str)
	{
		Encoding encoding = Encoding.UTF8;
		return encoding.GetBytes(str);
	}

	// Update is called once per frame. Used only for interpolation, but effectively replaced by UpdatePosition
	public void Update () {
		float step = SPEED * Time.deltaTime;

		//Interpolated (smooth)
		//transform.position = Vector3.MoveTowards(transform.position, nextPosition, step);

		//Not Interpolated
		transform.position = nextPosition;
	}

		// Use this for initialization (UNUSED, REPLACED BY Begin for On-Demand Calling)
	public void Start () {
	}
}
