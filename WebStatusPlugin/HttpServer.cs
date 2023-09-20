using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Diagnostics;

public class HttpProcessor {
	public TcpClient socket;        
	public HttpServer srv;
	
	private Stream inputStream;
	public StreamWriter outputStream;
	
	public String http_method;
	public String http_url;
	public String http_protocol_versionstring;
	public Hashtable httpHeaders = new Hashtable();
	private static int MAX_POST_SIZE = 64 * 1024; // 64kB
	
	public HttpProcessor(TcpClient s, HttpServer srv) {
		this.socket = s;
		this.srv = srv;                   
	}
	
	private string streamReadLine(Stream inputStream) {
		int next_char;
		string data = "";
		while (true) {
			next_char = inputStream.ReadByte();
			if (next_char == '\n') { break; }
			if (next_char == '\r') { continue; }
			if (next_char == -1) { Thread.Sleep(1); continue; };
			data += Convert.ToChar(next_char);
		}            
		return data;
	}
	public void process() {                        
		// we can't use a StreamReader for input, because it buffers up extra data on us inside it's
		// "processed" view of the world, and we want the data raw after the headers
		inputStream = new BufferedStream(socket.GetStream());
		
		// we probably shouldn't be using a streamwriter for all output from handlers either
		outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));
		try {
			parseRequest();
			readHeaders();
			if (http_method.Equals("GET")) {
				handleGETRequest();
			} else if (http_method.Equals("POST")) {
				handlePOSTRequest();
			}
		} catch (Exception e) {
			Console.WriteLine("Exception: " + e.ToString());
			writeFailure();
		}
		outputStream.Flush();
		// bs.Flush(); // flush any remaining output
		inputStream = null; outputStream = null; // bs = null;            
		socket.Close();             
	}
	
	public void parseRequest() {
		String request = streamReadLine(inputStream);
		string[] tokens = request.Split(' ');
		if (tokens.Length != 3) {
			throw new Exception("invalid http request line");
		}
		http_method = tokens[0].ToUpper();
		http_url = tokens[1];
		http_protocol_versionstring = tokens[2];
		
		//Console.WriteLine("starting: " + request);
        //Console.WriteLine( "Url: " + http_url );
	}
	
	public void readHeaders() {
		//Console.WriteLine("readHeaders()");
		String line;
		while ((line = streamReadLine(inputStream)) != null) {
			if (line.Equals("")) {
				//Console.WriteLine("got headers");
				return;
			}
			
			int separator = line.IndexOf(':');
			if (separator == -1) {
				throw new Exception("invalid http header line: " + line);
			}
			String name = line.Substring(0, separator);
			int pos = separator + 1;
			while ((pos < line.Length) && (line[pos] == ' ')) {
				pos++; // strip any spaces
			}
			
			string value = line.Substring(pos, line.Length - pos);
			//Console.WriteLine("header: {0}:{1}",name,value);
			httpHeaders[name] = value;
		}
	}
	
	public void handleGETRequest() {
		srv.handleGETRequest(this);
	}
	
	private const int BUF_SIZE = 4096;
	public void handlePOSTRequest() {
		// this post data processing just reads everything into a memory stream.
		// this is fine for smallish things, but for large stuff we should really
		// hand an input stream to the request processor. However, the input stream 
		// we hand him needs to let him see the "end of the stream" at this content 
		// length, because otherwise he won't know when he's seen it all! 
		
		Console.WriteLine("get post data start");
		int content_len = 0;
		MemoryStream ms = new MemoryStream();
		if (this.httpHeaders.ContainsKey("Content-Length")) {
			content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
			if (content_len > MAX_POST_SIZE) {
				throw new Exception(
					String.Format("POST Content-Length({0}) too big for this simple server",
				              content_len));
			}
			byte[] buf = new byte[BUF_SIZE];              
			int to_read = content_len;
			while (to_read > 0) {  
				Console.WriteLine("starting Read, to_read={0}",to_read);
				
				int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
				Console.WriteLine("read finished, numread={0}", numread);
				if (numread == 0) {
					if (to_read == 0) {
						break;
					} else {
						throw new Exception("client disconnected during post");
					}
				}
				to_read -= numread;
				ms.Write(buf, 0, numread);
			}
			ms.Seek(0, SeekOrigin.Begin);
		}
		Console.WriteLine("get post data end");
		srv.handlePOSTRequest(this, new StreamReader(ms));
		
	}
	
	public void writeSuccess(string content_type="text/html") {
		outputStream.WriteLine("HTTP/1.0 200 OK");            
		outputStream.WriteLine("Content-Type: " + content_type);
		outputStream.WriteLine("Connection: close");
		outputStream.WriteLine("");
	}
	
	public void writeFailure() {
		outputStream.WriteLine("HTTP/1.0 404 File not found");
		outputStream.WriteLine("Connection: close");
		outputStream.WriteLine("");
	}
}

public abstract class HttpServer {
	
	protected int port;
	TcpListener listener;
	bool is_active = true;
	Thread m_listenThread;
	protected IStringProvider m_stringProvider;
	
	public HttpServer(int port, IStringProvider stringProvider) {
		this.port = port;
		this.m_stringProvider = stringProvider;
	}

	public void Start ()
	{
		if ( m_listenThread != null ) 
		{
			return;
		}

		m_listenThread = new Thread(new ThreadStart( this.listen ) );
		m_listenThread.Start();
	}

	//Note: this currently does not forcefully shutdown the blocking listener...
	public void Stop()
	{
		is_active = false;
		try
		{
			if (listener != null)
			{
				listener.Stop();
			}
		}catch( Exception ) { }
	}
	
	public void listen() {
		listener = new TcpListener( IPAddress.Any, port );
		listener.Start();
		while (is_active) {                
			TcpClient s = listener.AcceptTcpClient();
			HttpProcessor processor = new HttpProcessor(s, this );
			Thread thread = new Thread(new ThreadStart(processor.process));
			thread.Start();
			Thread.Sleep(1);
		}
	}
	
	public abstract void handleGETRequest(HttpProcessor p);
	public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
}

public class HttpService : HttpServer {

	public HttpService(int port, IStringProvider stringProvider)
	: base(port, stringProvider) {
		
	}

    //Currently windows-only

		/*
    void DisplayExceptions( HttpProcessor p )
    {
        p.outputStream.WriteLine("<table><tr><td valign=top>");

        DatabaseService.Exception[] exResults = m_databaseService.QuerySelectRecentExceptions(); //crazy sauce...
        p.outputStream.WriteLine("<h2>Exceptions</h2>");
        if (exResults.Length > 0)
        {
            p.outputStream.WriteLine("<table border=1 style=\"border-collapse:collapse;\" cellpadding=2 cellspacing=2>");
            p.outputStream.Write("<tr><td style=\"background-color:lightGrey\">Date</td>");
            p.outputStream.Write("<td style=\"background-color:lightGrey\">User/Build</td>");
            p.outputStream.Write("<td style=\"background-color:lightGrey\">Message</td> ");
            //p.outputStream.WriteLine ("<td style=\"background-color:lightGrey\">Callstack</td></tr>");
            p.outputStream.WriteLine("</tr>");
            foreach (DatabaseService.Exception ex in exResults)
            {
                p.outputStream.WriteLine("<tr>");
                p.outputStream.Write("<td valign=top>");
                p.outputStream.Write(ex.Timestamp.ToLongDateString() + "<br>" + ex.Timestamp.ToLongTimeString());
                p.outputStream.Write("</td>");
                p.outputStream.Write("<td valign=top>");
                p.outputStream.Write(ex.Username);
                p.outputStream.Write("<br>");
                p.outputStream.Write(ex.Buildnumber);
                p.outputStream.Write("</td>");
                p.outputStream.Write("<td valign=top>");
                p.outputStream.Write(ex.Message);

                p.outputStream.Write("<table>");
                p.outputStream.Write("<tr>");

                p.outputStream.Write("<td valign=top>");
                p.outputStream.Write("<p style=\"font-size:12px\">");
                p.outputStream.Write(ex.Scenename);
                p.outputStream.Write("</p>");
                p.outputStream.Write("</td>");
                p.outputStream.Write("</tr>");

                p.outputStream.Write("<tr>");
                p.outputStream.Write("<td valign=top>");
                p.outputStream.Write("<p style=\"font-size:10px\">");
                string callstack = ex.Callstack.Replace(")", ")<br>");
                p.outputStream.Write(callstack);
                p.outputStream.Write("</p>");
                p.outputStream.Write("</td>");
                p.outputStream.Write("</tr>");
                p.outputStream.Write("</table>");

                p.outputStream.WriteLine("</tr>");
            }
            p.outputStream.WriteLine("</table>");
        }
    }
	*/

    void DisplayHeader(HttpProcessor p)
    {
        p.outputStream.WriteLine ("<html><body><h1>BuildCaddy Http Server</h1>");
		p.outputStream.WriteLine ("Current Time: " + DateTime.Now.ToString ());
		p.outputStream.WriteLine ("<hr>");
    }

    void DisplayFooter(HttpProcessor p)
    {
        p.outputStream.WriteLine("</td></tr></table>");
        p.outputStream.WriteLine("</body></html>");
    }

  
    public override void handleGETRequest(HttpProcessor p)
    {
        ProcessManually( p );
    }

    public void ProcessManually(HttpProcessor p)
	{
		p.writeSuccess ();
        DisplayHeader(p);

		if ( m_stringProvider != null )
		{
			p.outputStream.WriteLine( m_stringProvider.GetString() );
		}
		/*
            p.outputStream.WriteLine("<table><tr>");
            p.outputStream.WriteLine("<td valign=top width=\"2%\">");
            //DisplayExceptions(p);
            p.outputStream.WriteLine("</td><td valign=top>");
				p.outputStream.WriteLine("<h2>Hello World!</h2>");
			p.outputStream.WriteLine("</td>");

		p.outputStream.WriteLine("</hr></table>");
		*/
        DisplayFooter(p);
	}
	
	public override void handlePOSTRequest( HttpProcessor p, StreamReader inputData ) {	
		p.writeFailure();
	}
}

