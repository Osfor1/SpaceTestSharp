using Godot;
using System;
using System.Text;

public class HudLabel : RichTextLabel
{
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        var sb = new StringBuilder();
		sb.Append("HUD");
        sb.AppendLine();

		sb.Append("Speed: ");
        sb.Append(Math.Round(Player.PlayerVelocity, 1) / 10);
        sb.Append(" m/s");
        sb.AppendLine();

        sb.Append("Health: ");
        //sb.Append(Player.Health < 0 ? 0 : Player.PlayerHealth);
        sb.AppendLine();

        this.Text = sb.ToString();
    }
}
