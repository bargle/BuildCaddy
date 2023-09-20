using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using BuildCaddyShared;

public class Plugin : IPlugin, IBuildMonitor
{
  private IBuilder m_builder;
  private NetworkService m_networkService;
  private Dictionary<string, string> m_Config = new Dictionary<string, string>();

  Thread m_thread;
  bool m_bDone = false;
  IPEndPoint m_server = null;

  #region IPlugin Interface
  public void Initialize(IBuilder builder)
  {
    m_builder = builder;

    string cfg_filename = m_builder.GetConfigFilePath("network.cfg");
    if (!Config.ReadJSONConfig(cfg_filename, ref m_Config))
    {
      m_builder.GetLog().WriteLine("Error loading network.cfg! Networkplugin disabled...");
      return;
    }

    string enabled = GetConfigSetting("enabled");
    if (enabled.ToLower().CompareTo("true") != 0)
    {
      m_builder.GetLog().WriteLine("Networkplugin disabled in config...");
      return;
    }

    m_builder.AddBuildMonitor(this);

    m_networkService = new EncryptedNetworkService();
    m_networkService.Initialize(0, OnReceiveData);

    ThreadStart threadStart = new ThreadStart(DoWork);

    m_thread = new Thread(threadStart);
    m_thread.Start();
  }

  public void Shutdown()
  {
    m_bDone = true;
    m_networkService.Shutdown();
  }

  public string GetName() { return "NetworkPlugin"; }
  #endregion

  #region IBuildMonitor Interface
  public void OnRunning(string message)
  {
    Message newMsg = new Message();
    newMsg.Add("OP", "STATUS");
    newMsg.Add("message", message);
    m_networkService.Send(newMsg.GetSendable(), m_server);
  }

  public void OnStep(string message)
  {
    if (m_server == null)
    {
      return;
    }

    Message newMsg = new Message();
    newMsg.Add("OP", "STEP");
    newMsg.Add("message", message);
    m_networkService.Send(newMsg.GetSendable(), m_server);
  }

  public void OnSuccess(string message)
  {
    Message newMsg = new Message();
    newMsg.Add("OP", "STATUS");
    newMsg.Add("message", message);
    m_networkService.Send(newMsg.GetSendable(), m_server);
  }

  public void OnFailure(string message, string logFilename)
  {
    Message newMsg = new Message();
    newMsg.Add("OP", "STATUS");
    newMsg.Add("message", message);
    m_networkService.Send(newMsg.GetSendable(), m_server);
  }
  #endregion

  void DoWork()
  {
    while (true)
    {
      if (m_bDone)
      {
        break;
      }

      //if ( m_server == null )
      {
        Message newMsg = new Message();
        newMsg.Add("OP", "HLO");
        newMsg.Add("name", m_builder.GetName());

        try
        {
          int port = 0;
          string _port = GetConfigSetting("port");
          if (int.TryParse(_port, out port))
          {
            m_networkService.Send(newMsg.GetSendable(), new IPEndPoint(IPAddress.Broadcast, port));
          }
        }
        catch (System.Exception)
        {
        }
      }

      Thread.Sleep(10000); //1 second delay.. For now...
    }
  }

  void OnReceiveData(IMessage msg, IPEndPoint endPoint)
  {
    string op = msg.GetOperation();

    if (op.CompareTo("SRV") == 0)
    {
      if (m_server == null || !m_server.Equals(endPoint))
      {
        m_server = endPoint;
        Console.WriteLine("Server joined. " + endPoint.ToString());
      }
    }

    if (op.CompareTo("PNG") == 0)
    {
      if (m_server.Equals(endPoint))
      {
        Message newMsg = new Message();
        newMsg.Add("OP", "PONG");
        newMsg.Add("name", m_builder.GetName());
        m_networkService.Send(newMsg.GetSendable(), endPoint);
      }
    }

    if (op.CompareTo("BLD") == 0)
    {
      if (m_server.Equals(endPoint))
      {
        string rev = msg.GetValue("rev");
        m_builder.QueueCommand("build", new string[] { rev });
      }
    }

    if (op.CompareTo("DBLD") == 0)
    {
      if (m_server.Equals(endPoint))
      {
        string rev = msg.GetValue("rev");
        m_builder.DequeueCommand("build", new string[] { rev });
      }
    }

    //...
    //Console.WriteLine( GetName() + "received a message from: " +endPoint.ToString() );
    //Console.WriteLine( " MSG: " + msg.GetMessage() );
  }

  string GetConfigSetting(string key)
  {
    if (!m_Config.ContainsKey(key))
    {
      return string.Empty;
    }

    return m_Config[key];
  }
}
