using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Terraria;
using TShockAPI;

namespace TShockLauncher.Tests;

public class SpawnCommandTests
{
	[TestCase("spawnmob", "zombie")]
	[TestCase("spawnboss", "eye")]
	public void RestCallerNeedsAmountAndOnlinePlayer(string commandName, string npcName)
	{
		var command = Commands.ChatCommands.Single(c => c.Name == commandName);
		Assert.That(command.AllowServer, Is.True);

		var caller = new TSRestPlayer("api", new SuperAdminGroup());
		Commands.HandleCommand(caller, $"/{commandName} {npcName} 1");
		Assert.That(caller.GetCommandOutput(), Has.Some.Contains("online player"));

		caller = new TSRestPlayer("api", new SuperAdminGroup());
		Commands.HandleCommand(caller, $"/{commandName} {npcName} 1 MissingPlayer");
		Assert.That(caller.GetCommandOutput(), Has.Some.Contains("Invalid player"));

		caller = new TSRestPlayer("api", new SuperAdminGroup());
		Commands.HandleCommand(caller, $"/{commandName} {npcName} Alice");
		Assert.That(caller.GetCommandOutput(), Has.Some.Contains(commandName == "spawnmob" ? "Invalid syntax" : "Invalid boss amount"));
	}

	[TestCase("spawnmob", "unknownmob", "Invalid mob type")]
	[TestCase("spawnboss", "unknownboss", "Invalid boss type")]
	public void NamedPlayerIsResolvedForRestCaller(string commandName, string npcName, string expectedError)
	{
		const int playerIndex = 1;
		Player previousPlayer = Main.player[playerIndex];
		TSPlayer previousTSPlayer = TShock.Players[playerIndex];
		try
		{
			Main.player[playerIndex] = new Player { name = "Alice", active = true };
			TShock.Players[playerIndex] = new TSPlayer(playerIndex);

			var caller = new TSRestPlayer("api", new SuperAdminGroup());
			Commands.HandleCommand(caller, $"/{commandName} {npcName} 1 alice");

			Assert.That(caller.GetCommandOutput(), Has.Some.Contains(expectedError));
		}
		finally
		{
			Main.player[playerIndex] = previousPlayer;
			TShock.Players[playerIndex] = previousTSPlayer;
		}
	}

	[TestCase("spawnmob", "unknownmob", "Invalid mob type")]
	[TestCase("spawnboss", "unknownboss", "Invalid boss type")]
	public void InGameCallerCanOmitTarget(string commandName, string npcName, string expectedError)
	{
		const int playerIndex = 1;
		Player previousPlayer = Main.player[playerIndex];
		try
		{
			Main.player[playerIndex] = new Player { name = "Alice", active = true };
			var caller = new CapturingPlayer(playerIndex);
			var command = Commands.ChatCommands.Single(c => c.Name == commandName);
			command.CommandDelegate(new CommandArgs(commandName, caller, new List<string> { npcName }));

			Assert.That(caller.Messages, Has.Some.Contains(expectedError));
		}
		finally
		{
			Main.player[playerIndex] = previousPlayer;
		}
	}

	private class CapturingPlayer : TSPlayer
	{
		public List<string> Messages { get; } = new();

		public CapturingPlayer(int index) : base(index) { }

		public override void SendErrorMessage(string message) => Messages.Add(message);
	}
}
