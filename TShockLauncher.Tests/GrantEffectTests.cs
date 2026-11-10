using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace TShockLauncher.Tests;

public class GrantEffectTests
{
	[TestCase("\"Obsidian Skin\"")]
	[TestCase("1")]
	public void RestCommandGrantsEffectForSeconds(string effect)
	{
		const int playerIndex = 1;
		Player previousPlayer = Main.player[playerIndex];
		TSPlayer previousTSPlayer = TShock.Players[playerIndex];
		try
		{
			Main.player[playerIndex] = new Player { name = "Alice", active = true };
			var target = new CapturingPlayer(playerIndex);
			TShock.Players[playerIndex] = target;

			var command = Commands.ChatCommands.Single(c => c.Name == "granteffect");
			Assert.That(command.AllowServer, Is.True);
			Assert.That(command.Permissions, Does.Contain(Permissions.buffplayer));

			var caller = new TSRestPlayer("api", new SuperAdminGroup());
			Commands.HandleCommand(caller, $"/granteffect {effect} alice 30");

			Assert.Multiple(() =>
			{
				Assert.That(target.BuffType, Is.EqualTo(BuffID.ObsidianSkin));
				Assert.That(target.BuffTicks, Is.EqualTo(1800));
				Assert.That(caller.GetCommandOutput(), Has.Some.Contains("Granted"));
			});
		}
		finally
		{
			Main.player[playerIndex] = previousPlayer;
			TShock.Players[playerIndex] = previousTSPlayer;
		}
	}

	[TestCase("0")]
	[TestCase("-1")]
	[TestCase("abc")]
	[TestCase("35791394")]
	public void RestCommandRejectsInvalidDuration(string seconds)
	{
		var caller = new TSRestPlayer("api", new SuperAdminGroup());
		Commands.HandleCommand(caller, $"/granteffect Ironskin Alice {seconds}");
		Assert.That(caller.GetCommandOutput(), Has.Some.Contains("Seconds must be a positive whole number"));
	}

	[Test]
	public void RestCommandRequiresOnlinePlayer()
	{
		var caller = new TSRestPlayer("api", new SuperAdminGroup());
		Commands.HandleCommand(caller, "/granteffect Ironskin MissingPlayer 30");
		Assert.That(caller.GetCommandOutput(), Has.Some.Contains("Invalid player"));
	}

	[Test]
	public void ListIncludesEveryVanillaBuffId()
	{
		var caller = new TSRestPlayer("api", new SuperAdminGroup());
		Commands.HandleCommand(caller, "/granteffect list");
		Assert.Multiple(() =>
		{
			Assert.That(caller.GetCommandOutput(), Has.Count.EqualTo(BuffID.Count - 1));
			Assert.That(caller.GetCommandOutput(), Has.Some.Contains("Obsidian Skin"));
		});
	}

	private class CapturingPlayer : TSPlayer
	{
		public int BuffType { get; private set; }
		public int BuffTicks { get; private set; }
		public List<string> Messages { get; } = new();

		public CapturingPlayer(int index) : base(index) { }

		public override void SetBuff(int type, int time = 3600, bool bypass = false)
		{
			BuffType = type;
			BuffTicks = time;
		}

		public override void SendSuccessMessage(string message) => Messages.Add(message);
	}
}
