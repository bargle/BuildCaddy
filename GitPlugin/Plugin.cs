﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

using BuildCaddyShared;

public class Plugin : IPlugin
{
    private IBuilder m_builder;
    private Dictionary<string, string> m_Config = new Dictionary<string, string>();
    private Thread m_thread;
    private bool m_bDone = false;
    private int m_delay = 10000;
    private string m_repo;
    private string m_branch = "master";

    #region IPlugin Interface
    public void Initialize(IBuilder builder)
    {
        m_builder = builder;

        string cfg_filename = m_builder.GetConfigFilePath("git.cfg");
        if (!Config.ReadJSONConfig(cfg_filename, ref m_Config))
        {
            m_builder.GetLog().WriteLine("Error loading svn.cfg! " + GetName() + " disabled...");
            return;
        }

        string enabled = GetConfigSetting("enabled");
        if (enabled.ToLower().CompareTo("true") != 0)
        {
            m_builder.GetLog().WriteLine( GetName() + " disabled in config...");
            return;
        }

        m_repo = m_builder.GetConfigString("repo");
        Console.WriteLine("Repo: " + m_repo);

        string branch = m_builder.GetConfigString("branch");
        if ( branch.Length != 0 )
        {
            m_branch = branch;
        }

        string _delay = GetConfigSetting("delay");
        if (int.TryParse(_delay, out m_delay))
        {
            ThreadStart threadStart = new ThreadStart(DoWork);
            m_thread = new Thread(threadStart);
            m_thread.Start();
        }
        else
        {
            m_builder.GetLog().WriteLine("Error parsing delay! " + GetName() + " disabled...");
        }
    }

    public void Shutdown()
    {
        m_bDone = true;
    }

    public string GetName()
    {
        return "GitPlugin";
    }
    #endregion

    string GetConfigSetting(string key)
    {
        if (!m_Config.ContainsKey(key))
        {
            return string.Empty;
        }

        return m_Config[key];
    }

    string GetAndUpdateRevisionNumber(string url, string branch)
    {
        ProcessStartInfo start = new ProcessStartInfo();
        //start.WorkingDirectory = url;
        start.FileName = GetConfigSetting("GIT_BINARY");
        start.Arguments = "ls-remote " + url;
        start.UseShellExecute = false;
        start.RedirectStandardOutput = true;

        using (Process process = Process.Start(start))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string result = reader.ReadToEnd();
                string[] tokens = result.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string token in tokens)
                {
                    if ( token.Contains( "refs/heads/" + branch ) )
                    {
                        string[] revisionTokens = token.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (revisionTokens.Length > 1)
                        {
                            return revisionTokens[ 0 ];
                        }
                    }
                }
            }
        }

        return string.Empty;
    }

    void DoWork()
    {
        Console.WriteLine("GIT: initial check...");
        string current_rev = GetAndUpdateRevisionNumber(m_repo, m_branch);
        Console.WriteLine("GIT: current rev: " + current_rev);

        while (true)
        {
            if (m_bDone)
            {
                break;
            }

            Thread.Sleep(m_delay);

            Console.WriteLine("GIT: checking...");
            string rev = GetAndUpdateRevisionNumber(m_repo, m_branch);
            Console.WriteLine("GIT - rev: " + rev);

            if (rev == string.Empty)
            {
                continue;
            }

            if (rev.CompareTo(current_rev) != 0)
            {
                Console.WriteLine( "New commit: " + rev );

                m_builder.QueueCommand( "build", new string[] { rev } );

                current_rev = rev;
            }

        }
    }
}

