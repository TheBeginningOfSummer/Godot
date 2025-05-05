using Godot;
using System.Collections.Generic;

public partial class Player : CharacterBody2D
{
    #region 参数
    public const float Speed = 160.0f;
    public const float Acceleration = Speed / 0.2f;
    public const float JumpVelocity = -400.0f;
    public readonly Vector2 WallJumpVelocity = new(100, -400);
    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    public HashSet<PlayerState> OnGround = [PlayerState.Idle, PlayerState.Running, PlayerState.Landing];
    #endregion

    #region 组件
    public Sprite2D PlayerSprite;
    public AnimationPlayer PlayerAnimation;
    public Timer CoyoteJump;
    public Timer RequestJump;
    public RayCast2D TopRayCast;
    #endregion

    #region 变量
    private Vector2 velocity = Vector2.Zero;

    private PlayerState _state = PlayerState.None;
    public PlayerState CurrentState
    {
        get { return _state; }
        set
        {
            TransitionState(_state, value);
            _state = value;
        }
    }
    #endregion

    public override void _Ready()
    {
        PlayerSprite = GetNode<Sprite2D>("Sprite2D");
		PlayerAnimation = GetNode<AnimationPlayer>("AnimationPlayer");
        CoyoteJump = GetNode<Timer>("CoyoteJump");
        RequestJump = GetNode<Timer>("RequestJump");
        TopRayCast = GetNode<RayCast2D>("TopRayCast2D");
        CurrentState = PlayerState.Idle;
    }

    public override void _PhysicsProcess(double delta)
    {
        CurrentState = GetNextState(CurrentState, PlayerMove(CurrentState, delta));//更新状态和动画
        MoveAndSlide();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
            RequestJump.Start();
        if (@event.IsActionReleased("ui_accept"))
        {
            if (velocity.Y < JumpVelocity / 2)
                velocity.Y = JumpVelocity / 2;
        }
    }

    public bool PlayerMove(PlayerState state, double delta)
    {
        #region 跳跃
        if (!IsOnFloor() && state != PlayerState.WallSlidingL && state != PlayerState.WallSlidingR)
            velocity.Y += gravity * (float)delta;
        //滑墙状态下，重力减小
        if (state == PlayerState.WallSlidingL || state == PlayerState.WallSlidingR)
            velocity.Y += (gravity / 5) * (float)delta;
        var canJump = IsOnFloor() || CoyoteJump.TimeLeft > 0;
        bool isJump = canJump && RequestJump.TimeLeft > 0;
        if (isJump) velocity.Y = JumpVelocity;
        //头碰到障碍
        if (TopRayCast.IsColliding())
        {
            if (velocity.Y < 0) velocity.Y = 0;
            velocity.Y += gravity * (float)delta;
        }
        #endregion

        #region 平移
        var direction = Input.GetAxis("ui_left", "ui_right");
        //水平位置更新
        velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * (float)delta);
        //角色换向
        if (direction != 0) PlayerSprite.FlipH = direction < 0;
        #endregion

        //更新角色位移
        Velocity = velocity;

        return isJump;
    }
    /// <summary>
    /// 位置与状态更新
    /// </summary>
    /// <param name="state">之前的状态</param>
    /// <param name="delta">每帧的时间</param>
    /// <returns>更新后的状态</returns>
    public PlayerState GetNextState(PlayerState state, bool isJump)
    {
        if (isJump) return PlayerState.Jump;

        switch (state)
        {
            case PlayerState.Idle:
                if (!IsOnFloor()) return PlayerState.Fall;
                if (Velocity.X != 0) return PlayerState.Running;
                break;
            case PlayerState.Running:
                if (!IsOnFloor()) return PlayerState.Fall;
                if (Velocity.X == 0) return PlayerState.Idle;
                break;
            case PlayerState.Jump:
                if (Velocity.Y >= 0) return PlayerState.Fall;
                break;
            case PlayerState.Fall:
                if (IsOnFloor()) return Velocity.X == 0 ? PlayerState.Landing : PlayerState.Running;
                if (IsOnWall())
                {
                    if (GetWallNormal().X > 0)
                        return PlayerState.WallSlidingL;
                    else
                        return PlayerState.WallSlidingR;
                }
                break;
            case PlayerState.Landing:
                if (!PlayerAnimation.IsPlaying()) return PlayerState.Idle;
                break;
            case PlayerState.WallSlidingL:
                if (RequestJump.TimeLeft > 0) return PlayerState.WallJump;
                if (IsOnFloor()) return PlayerState.Idle;
                if (!IsOnWall()) return PlayerState.Fall;
                break;
            case PlayerState.WallSlidingR:
                if (RequestJump.TimeLeft > 0) return PlayerState.WallJump;
                if (IsOnFloor()) return PlayerState.Idle;
                if (!IsOnWall()) return PlayerState.Fall;
                break;
            case PlayerState.WallJump:
                if (Velocity.Y >= 0) return PlayerState.Fall;
                break;
            default:
                break;
        }
        return state;
    }
    /// <summary>
    /// 更新动画
    /// </summary>
    /// <param name="preState">之前的状态</param>
    /// <param name="currentState">当前的状态</param>
    public void TransitionState(PlayerState preState, PlayerState currentState)
    {
        if (!OnGround.Contains(preState) && OnGround.Contains(currentState)) CoyoteJump.Stop();
        switch (currentState)
        {
            case PlayerState.Idle:
                PlayerAnimation.Play("idle");
                break;
            case PlayerState.Running:
                PlayerAnimation.Play("running");
                break;
            case PlayerState.Jump:
                PlayerAnimation.Play("jump");
                CoyoteJump.Stop();
                RequestJump.Stop();
                break;
            case PlayerState.Fall:
                PlayerAnimation.Play("fall");
                if (OnGround.Contains(preState)) CoyoteJump.Start();
                break;
            case PlayerState.Landing:
                PlayerAnimation.Play("landing");
                break;
            case PlayerState.WallSlidingL:
                if (preState != PlayerState.WallSlidingL) velocity.Y = 0;
                PlayerAnimation.Play("wallSlidingL");
                break;
            case PlayerState.WallSlidingR:
                if (preState != PlayerState.WallSlidingR) velocity.Y = 0;
                PlayerAnimation.Play("wallSlidingR");
                break;
            case PlayerState.WallJump:
                PlayerAnimation.Play("jump");
                velocity.Y = JumpVelocity;
                velocity.X *= GetWallNormal().X;
                RequestJump.Stop();
                break;
            default:
                break;
        }
    }
}

public enum PlayerState
{
    Idle,
    None,
    Running,
    Jump,
    Fall,
    Landing,
    WallSlidingL,
    WallSlidingR,
    WallJump
}
