using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Globalization;




public class CarController : MonoBehaviour {

	private static readonly int BASE_TTL = 5;
	private static readonly String pythonAddress = "127.0.0.1";
	private Socket pythonSocket;
	private byte[] receiveBuffer = new byte[1024];

	private string text;
	private StreamReader reader;

	private static readonly float minLat = 40.981F;
	private static readonly float maxLat = 41.399F;
	private static readonly float minLon = -8.795F;
	private static readonly float maxLon = -8.358F;

	static readonly Vector3 center=   new Vector3(41F, -8.6F,0.0F);


	private float imWidth;
	private float imHeight;

	private SpriteRenderer rend;


	// Use this for initialization
	public void Start () {
	}

	// Start on Demand
	public void Begin (String filename, float width, float height) {
		imWidth = width;
		imHeight = height;

		int port = 8000 + int.Parse (filename.Split ('.') [1]);
		File.WriteAllText (Application.dataPath+"/port.txt", port.ToString());

		//pythonSocket = GetSocket (port);

		rend = GetComponent<SpriteRenderer>();



		FileInfo f = new FileInfo (filename);
		reader = f.OpenText();

		text = "start";

		transform.position = ConvertCoords (center.x, center.y);

		// Use this to update every second, create UpdatePosition for that
		//InvokeRepeating("UpdatePosition", 0, 1.0F);

		//SpawnPython ();

		//pythonSocket.BeginReceive (receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback),null );

	}

	private void ReceiveCallback(IAsyncResult result){
		int receivedLength = pythonSocket.EndReceive (result);

		byte[] sizedData = new byte[receivedLength];
		Buffer.BlockCopy (receiveBuffer, 0, sizedData, 0, receivedLength);

		int ttl = BitConverter.ToInt32 (sizedData,0);

		//TODO: Handle this and broadcast to close ones.

		pythonSocket.BeginReceive (receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback),null );
	}




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

	// Update is called once per frame
	public void Update () {
		text = reader.ReadLine();

		if (text != null && text != "NOP") {
			string[] tokens = text.Split (' ');

			double coordX = double.Parse (tokens [0], CultureInfo.InvariantCulture.NumberFormat);
			double coordY = double.Parse (tokens [1], CultureInfo.InvariantCulture.NumberFormat);

			//Debug.Log("X: "+x+" Y: "+y);

			transform.position = ConvertCoords (coordX, coordY);

			if (int.Parse (tokens [2]) == 0)
				rend.color = new Color (0f, 1f, 0f);
			else {
				rend.color = new Color (1f, 0f, 0f);
				//pythonSocket.Send(StrToByteArray(coordX+" "+coordY));
				//pythonSocket.Send(BitConverter.GetBytes(BASE_TTL));
			}
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

}
