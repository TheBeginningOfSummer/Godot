using Godot;

public partial class Player : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public Sprite2D PlayerSprite;
	public AnimationPlayer PlayerAnimation;
	private Vector2 velocity = Vector2.Zero;

    public override void _Ready()
    {
		PlayerSprite = GetNode<Sprite2D>("Sprite2D");
		PlayerAnimation = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public override void _PhysicsProcess(double delta)
    {
        Velocity = PlayerMove(delta, Input.GetAxis("ui_left", "ui_right"), Input.IsActionJustPressed("ui_accept") && IsOnFloor());
        MoveAndSlide();
    }

    public Vector2 PlayerMove(double delta, float direction, bool isJump)
    {
        //位移
        if (isJump) velocity.Y = JumpVelocity;
        velocity.Y += gravity * (float)delta;
        if (direction == 0)
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        else
        {
            velocity.X = direction * Speed;
            //角色换向
            PlayerSprite.FlipH = direction < 0;
        }
        //动画
        if (IsOnFloor())
        {
            if (direction == 0)
                PlayerAnimation.Play("idle");
            else
                PlayerAnimation.Play("running");
        }
        else
        {
            PlayerAnimation.Play("jump");
        }
        return velocity;
    }
}
