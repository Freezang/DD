using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GameCore;
using HarmonyLib;
using Newtonsoft.Json;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using TheRiptide.Patches;

namespace TheRiptide;

public class Plugin
{
	[PluginConfig]
	public Config config;

	private Harmony harmony;

	private HttpClient client;

	public static Plugin Instance { get; private set; }

	[PluginEntryPoint("Anti-DDOS", "1.0.0", "", "The Riptide")]
	public void OnEnabled()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		Log.Info("Enabling Anti-DDoS", (string)null);
		Instance = this;
		harmony = new Harmony("TheRiptide.Anti-DDOS");
		if (config.Enabled)
		{
			EventManager.RegisterEvents((object)this);
			harmony.PatchAll();
			client = new HttpClient();
			Log.Info("Successfully Enabled Anti-DDoS", (string)null);
		}
		else
		{
			Log.Info("Anti-DDoS failed to start as its disabled in the Config", (string)null);
		}
	}

	[PluginUnload]
	public void OnDisabled()
	{
		Log.Info("Disabling Anti-DDoS", (string)null);
		harmony.UnpatchAll("TheRiptide.Anti-DDOS");
		harmony = null;
		EventManager.UnregisterAllEvents((object)this);
		((HttpMessageInvoker)client).Dispose();
		client = null;
		Instance = null;
		Log.Info("Successfully Disabled Anti-DDoS", (string)null);
	}

	[PluginEvent(/*Could not decode attribute arguments.*/)]
	public void OnRoundEnd(RoundEndEvent e)
	{
		if (NetManagerOnMessageReceivedPatch.BadDataCount <= 0)
		{
			return;
		}
		int count = NetManagerOnMessageReceivedPatch.BadDataCount;
		long size = NetManagerOnMessageReceivedPatch.BadDataBytes;
		NetManagerOnMessageReceivedPatch.BadDataCount = 0;
		NetManagerOnMessageReceivedPatch.BadDataBytes = 0L;
		Log.Info($"Potential DDoS detected\nPacket Count: {count}\nTotal Size: {BytesToString(size)}", (string)null);
		if (string.IsNullOrEmpty(config.DiscordWebHook))
		{
			Log.Info("Anti-DDoS config.DiscordWebHook empty", (string)null);
			return;
		}
		new Task(async delegate
		{
			string text = JsonConvert.SerializeObject((object)ServerName());
			string text2 = JsonConvert.SerializeObject((object)$"Potential DDoS detected\nPacket Count: {count}\nTotal Size: {BytesToString(size)}");
			StringContent val = new StringContent("\r\n{\r\n  \"username\": " + text + ",\r\n  \"content\": " + text2 + "\r\n}\r\n");
			((HttpContent)val).Headers.ContentType = new MediaTypeHeaderValue("application/json");
			HttpResponseMessage val2 = await client.PostAsync(config.DiscordWebHook, (HttpContent)(object)val);
			if (!val2.IsSuccessStatusCode)
			{
				Log.Error(await val2.Content.ReadAsStringAsync(), (string)null);
			}
		}).Start();
	}

	private static string BytesToString(long byteCount)
	{
		string[] array = new string[7] { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
		if (byteCount == 0L)
		{
			return "0" + array[0];
		}
		long num = Math.Abs(byteCount);
		int num2 = Convert.ToInt32(Math.Floor(Math.Log(num, 1024.0)));
		double num3 = Math.Round((double)num / Math.Pow(1024.0, num2), 1);
		return (double)Math.Sign(byteCount) * num3 + array[num2];
	}

	private static string ServerName()
	{
		string text = StripTagsCharArray(ConfigFile.ServerConfig.GetString("server_name", "My Server Name").Split(new char[1] { '\n' }).First()).Replace("Discord", "");
		if (string.IsNullOrEmpty(text))
		{
			text = "Server name Empty";
		}
		return text;
	}

	public static string StripTagsCharArray(string source)
	{
		char[] array = new char[source.Length];
		int num = 0;
		bool flag = false;
		foreach (char c in source)
		{
			switch (c)
			{
			case '<':
				flag = true;
				continue;
			case '>':
				flag = false;
				continue;
			}
			if (!flag)
			{
				array[num] = c;
				num++;
			}
		}
		return new string(array, 0, num);
	}
}
