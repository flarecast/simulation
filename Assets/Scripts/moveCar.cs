using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;




public class moveCar : MonoBehaviour {

	static readonly int BASE_TTL = 5;
	static readonly String pythonAddress = "127.0.0.1";
	Socket pythonSocket;
	byte[] receiveBuffer = new byte[1024];

	string text;
	StreamReader reader;

	/*  BRAGA
	static readonly Vector3 topLeft=  new Vector3(41.580077F, -8.494539F,0.0F);
	static readonly Vector3 topRight= new Vector3(41.514992F, -8.494539F,0.0F);
	static readonly Vector3 botRight= new Vector3(41.514992F, -8.364909F,0.0F);
	static readonly Vector3 botLeft=  new Vector3(41.580077F, -8.364909F,0.0F);
	static readonly Vector3 center=   new Vector3(41.5475345F, -8.429724F,0.0F);
	*/

	static readonly float minLat = 40.981F;
	static readonly float maxLat = 41.399F;
	static readonly float minLon = -8.795F;
	static readonly float maxLon = -8.358F;

	static readonly Vector3 center=   new Vector3(41F, -8.6F,0.0F);



	int imHoriz = 1572;
	int imVert = 2000;

	SpriteRenderer rend;


	// Use this for initialization
	void Start () {
	}

	// Start on Demand
	void Begin (String filename) {
		int port = 8000 + int.Parse (filename.Split ('.') [1]);
		File.WriteAllText (Application.dataPath+"/port.txt", port.ToString());

		//pythonSocket = GetSocket (port);

		rend = GetComponent<SpriteRenderer>();



		FileInfo f = new FileInfo (filename);
		reader = f.OpenText();

		text = "start";

		transform.position = ConvertCoords (center.x, center.y);

		//InvokeRepeating("UpdatePosition", 0, 1.0F);

		SpawnPython ();

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

	void UpdatePosition (){
		text = reader.ReadLine();
		if (text == null)
			CancelInvoke();

		if (text != "NOP") {
			string[] tokens = text.Split (' ');

			float coordX = float.Parse (tokens [0]);
			float coordY = float.Parse (tokens [1]);

			//Debug.Log("X: "+x+" Y: "+y);

			transform.position = ConvertCoords (coordX, coordY);

			if (int.Parse (tokens [2]) == 0)
				rend.color = new Color (0f, 1f, 0f);
			else {
				rend.color = new Color (1f, 0f, 0f);
				//pythonSocket.Send(StrToByteArray(coordX+" "+coordY));
				//pythonSocket.Send("event!"+BASE_TTL);
			}
		}
	}



	void SpawnPython(){
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
	void Update () {
		text = reader.ReadLine();

		if (text != null && text != "NOP") {
			string[] tokens = text.Split (' ');

			float coordX = float.Parse (tokens [0]);
			float coordY = float.Parse (tokens [1]);

			//Debug.Log("X: "+x+" Y: "+y);

			transform.position = ConvertCoords (coordX, coordY);

			if (int.Parse (tokens [2]) == 0)
				rend.color = new Color (0f, 1f, 0f);
			else {
				rend.color = new Color (1f, 0f, 0f);
				//pythonSocket.Send(StrToByteArray(coordX+" "+coordY));
				pythonSocket.Send(BitConverter.GetBytes(BASE_TTL));
			}
		}
	}

	Vector3 ConvertCoords (float lat, float lon){
		float proportionX = (lon - minLon) / (maxLon - minLon);
		float proportionY = (lat - minLat) / (maxLat - minLat);

		float x = (proportionX * imHoriz) - (imHoriz / 2);
		float y = (proportionY * imVert) - (imVert / 2);

		return ( new Vector3( x , y , 0.0F));
	}

	Socket GetSocket(int port){
		IPEndPoint ipe = new IPEndPoint (IPAddress.Parse(pythonAddress), port);
		Socket tempSocket = 
			new Socket (ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

		tempSocket.Connect (ipe);

		if (tempSocket.Connected)
			return tempSocket;
		else
			return null;
	}

	public static byte[] StrToByteArray(string str)
	{
		Encoding encoding = Encoding.UTF8;
		return encoding.GetBytes(str);
	}
}
