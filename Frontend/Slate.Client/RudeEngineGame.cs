using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Font;
using MLEM.Misc;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Style;
using MonoScene.Graphics;
using MonoScene.Graphics.Pipeline;
using Slate.Client.Services;
using Slate.Client.UI;
using Slate.Client.UI.Elements;
using Slate.Client.UI.Views;
using Stateless;
using CharacterListViewModel = Slate.Client.ViewModel.MainMenu.CharacterListViewModel;
using Keyboard = Microsoft.Xna.Framework.Input.Keyboard;
using LoginViewModel = Slate.Client.ViewModel.MainMenu.LoginViewModel;

namespace Slate.Client
{
    public class RudeEngineGame : Microsoft.Xna.Framework.Game
    {
	    private TaskCompletionSource<GameTime> _thisUpdateSource = new();
        public static Task<GameTime> NextUpdate = null!; //static will be initialized in constructor

        private readonly Options _options;
        
        private readonly PBREnvironment _lightsAndFog = PBREnvironment.CreateDefault();
        private DeviceModelCollection _testModel;
        private readonly ModelInstance[] _test = new ModelInstance[5 * 5];
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private UiSystem _uiSystem;
        private GameLifecycle _gameLifecycle;

        public RudeEngineGame(Options options)
        {
            _options = options;
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 1366;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.PreparingDeviceSettings += graphics_PreparingDeviceSettings;
            Window.ClientSizeChanged += Window_ClientSizeChanged;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            NextUpdate = _thisUpdateSource.Task;
        }

        private void Window_ClientSizeChanged(object? sender, EventArgs e)
        {

        }

        private void graphics_PreparingDeviceSettings(object? sender, PreparingDeviceSettingsEventArgs e)
        {
            _graphics.PreferMultiSampling = true;
            _graphics.GraphicsProfile = GraphicsProfile.HiDef;
            _graphics.SynchronizeWithVerticalRetrace = true;
            _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
        }

        protected override void LoadContent()
        {
            _playerInput = this.AddComponentAndService(GameInputBindings.CreateInputBindings(this));
            this.AddComponentAndService<IDebugInfoSink>(new DebugInfoSink(this) { Enabled = true });
            Metrics.Install(this);
            var camera = new Camera(GraphicsDevice, Services.GetService<IDebugInfoSink>());
            Components.Add(camera);
            
            this.AddComponentAndService<CastIron.Engine.Camera.ICamera>(_camera);
            this.IsMouseVisible = true;

            SpriteFont font = Content.Load<SpriteFont>("Segoe_UI_15_Bold");
            //Viewport viewport = GraphicsDevice.Viewport;
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            var uiTexture = Content.Load<Texture2D>("UI/MockUI");
            var uiStyle = new UntexturedStyle(_spriteBatch)
            {
                Font = new GenericSpriteFont(font),
                TextFieldTexture = new NinePatch(new TextureRegion(uiTexture, 128, 216, 128, 32), 8, NinePatchMode.Stretch),
                PanelTexture = new NinePatch(new TextureRegion(uiTexture, 384, 128, 128, 128), 20, NinePatchMode.Stretch),
                ButtonTexture = new NinePatch(new TextureRegion(uiTexture, 128, 128, 128, 32), 8, NinePatchMode.Stretch),
                RadioTexture = new NinePatch(new TextureRegion(uiTexture, 128, 172, 32, 32), 0),
                RadioCheckmark = new TextureRegion(uiTexture, 192, 172, 32, 32)
            };

            uiStyle.Font = new GenericSpriteFont(font);
            _uiSystem = new UiSystem(this, uiStyle);

            var authService = new AuthService(_options.AuthServer);
            var gameConnection = new GameConnection(_options.GameServer, _options.GameServerPort, authService);
            _gameLifecycle = new GameLifecycle(_uiSystem, authService, gameConnection);
            _gameLifecycle.Start();



            var gltfFactory = new GltfModelFactory(GraphicsDevice);
            _testModel = gltfFactory.LoadModel(Path.Combine($"Content", "Cell100.glb"));
        }

        

        private async void LoginViewModelOnLoggedIn(object? sender, TokenResponse e)
        {
	        
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();

            _testModel?.Dispose();
            _testModel = null!;
        }

        protected override void Update(GameTime gameTime)
        {
	        var thisUpdate = _thisUpdateSource;
	        _thisUpdateSource = new();
	        NextUpdate = _thisUpdateSource.Task;
            thisUpdate.SetResult(gameTime);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (Keyboard.GetState().IsKeyDown(Keys.F3))
            {
                foreach (var reloadablePanel in _uiSystem.GetRootElements().Select(re => re.Element).OfType<ReloadablePanel>())
                {
                    reloadablePanel.Rebuild();
                }
            }
            
            _uiSystem.Update(gameTime);

            for (int z = 0; z < 5; ++z)
            {
                for (int x = 0; x < 5; ++x)
                {
                    var index = z * 5 + x;
                    _test[index] = _testModel.DefaultModel.CreateInstance();
                    _test[index].WorldMatrix = Matrix.CreateTranslation(x * 100, 0, z * 100);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _uiSystem.DrawEarly(gameTime, _spriteBatch);

            GraphicsDevice.Clear(Color.CornflowerBlue);
            
            var camPos = new Vector3(0, 25, 0);
            var modelPosition = new Vector3(50f, 0, 50f);

            var camX = Matrix.CreateWorld(Vector3.UnitY * 10, modelPosition - camPos, Vector3.UnitY);

            var dc = new ModelDrawingContext(this.GraphicsDevice)
            {
                NearPlane = 0.1f
            };
            
            dc.SetCamera(camX);
            
            dc.DrawSceneInstances(_lightsAndFog,
                _test);
            
            this._uiSystem.Draw(gameTime, _spriteBatch);

            base.Draw(gameTime);
        }
    }
}
