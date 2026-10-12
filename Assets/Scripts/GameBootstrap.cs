using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance;
    public int playerHealth = 100;
    public int kills;
    public int ammo = 30;
    public int reserveAmmo = 90;
    public bool started;
    private Transform player;
    private Camera cam;
    private CharacterController controller;
    private float pitch;
    private float yaw;
    private float fireCooldown;
    private Vector2 moveTouch;
    private Vector2 lookTouch;
    private bool fireHeld;
    private Text hudText;
    private GameObject menuRoot;
    private readonly List<BotAgent> bots = new List<BotAgent>();
    private readonly List<GameObject> touchControls = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        BuildArena();
        BuildPlayer();
        BuildBots();
        BuildUI();
        SetPlaying(false);
    }

    Material Mat(Color c)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new Material(shader);
        m.color = c;
        return m;
    }

    void BuildArena()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Arena Floor";
        floor.transform.position = new Vector3(0, -0.5f, 0);
        floor.transform.localScale = new Vector3(44, 1, 44);
        floor.GetComponent<Renderer>().material = Mat(new Color(.16f,.18f,.25f));

        var wallMat = Mat(new Color(.22f,.25f,.36f));
        for (int i = -2; i <= 2; i++)
        {
            MakeBlock(new Vector3(i * 7, 1.4f, 10), new Vector3(3, 2.8f, 1), wallMat);
            MakeBlock(new Vector3(i * 7, 1.4f, -10), new Vector3(3, 2.8f, 1), wallMat);
            MakeBlock(new Vector3(10, 1.4f, i * 7), new Vector3(1, 2.8f, 3), wallMat);
            MakeBlock(new Vector3(-10, 1.4f, i * 7), new Vector3(1, 2.8f, 3), wallMat);
        }
        var crateMat = Mat(new Color(.34f,.38f,.48f));
        for (int i = 0; i < 9; i++)
        {
            float x = (i % 3 - 1) * 5f;
            float z = (i / 3 - 1) * 5f;
            if (Mathf.Abs(x) < 1 && Mathf.Abs(z) < 1) continue;
            MakeBlock(new Vector3(x, .65f, z), new Vector3(1.7f, 1.3f, 1.7f), crateMat);
        }

        var lightObject = new GameObject("Arena Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.transform.rotation = Quaternion.Euler(50, -35, 0);
        RenderSettings.ambientLight = new Color(.48f,.50f,.62f);
    }

    void MakeBlock(Vector3 pos, Vector3 scale, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.transform.position = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().material = mat;
    }

    void BuildPlayer()
    {
        var p = new GameObject("Player");
        p.transform.position = new Vector3(0, .1f, -15);
        controller = p.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = .35f;
        player = p.transform;

        var cameraObject = new GameObject("Player Camera");
        cameraObject.transform.SetParent(player);
        cameraObject.transform.localPosition = new Vector3(0, 1.55f, 0);
        cam = cameraObject.AddComponent<Camera>();
        cam.fieldOfView = 78;
        cam.nearClipPlane = .05f;
        cam.backgroundColor = new Color(.08f,.10f,.17f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        var listener = cameraObject.AddComponent<AudioListener>();
        Cursor.lockState = CursorLockMode.None;
    }

    void BuildBots()
    {
        for (int i = 0; i < 5; i++)
        {
            var bot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bot.name = "Training Bot " + (i + 1);
            bot.transform.position = new Vector3((i - 2) * 3.2f, 1, 3 + (i % 2) * 3);
            bot.GetComponent<Renderer>().material = Mat(new Color(.75f,.20f,.29f));
            var agent = bot.AddComponent<BotAgent>();
            bots.Add(agent);
        }
    }

    void BuildUI()
    {
        var canvasObj = new GameObject("Mobile HUD");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        hudText = MakeText(canvas.transform, "HUD", "STRIKE ZONE  |  HP 100  |  30 / 90", 20, TextAnchor.UpperLeft, font);
        hudText.rectTransform.anchorMin = new Vector2(0,1);
        hudText.rectTransform.anchorMax = new Vector2(0,1);
        hudText.rectTransform.pivot = new Vector2(0,1);
        hudText.rectTransform.anchoredPosition = new Vector2(24,-20);
        hudText.rectTransform.sizeDelta = new Vector2(650,60);

        var cross = MakeText(canvas.transform, "Crosshair", "+", 30, TextAnchor.MiddleCenter, font);
        cross.rectTransform.anchorMin = cross.rectTransform.anchorMax = new Vector2(.5f,.5f);
        cross.rectTransform.sizeDelta = new Vector2(50,50);

        MakeTouchButton(canvas.transform, "FIRE", new Vector2(1,0), new Vector2(-105,125), () => Shoot());
        MakeTouchButton(canvas.transform, "RELOAD", new Vector2(1,0), new Vector2(-105,55), Reload);
        MakeTouchButton(canvas.transform, "LEFT", new Vector2(0,0), new Vector2(75,90), () => yaw -= 8);
        MakeTouchButton(canvas.transform, "RIGHT", new Vector2(0,0), new Vector2(165,90), () => yaw += 8);
        MakeTouchButton(canvas.transform, "UP", new Vector2(0,0), new Vector2(120,150), () => MoveForward(.8f));

        menuRoot = new GameObject("Main Menu", typeof(RectTransform), typeof(Image));
        menuRoot.transform.SetParent(canvas.transform, false);
        var rt = menuRoot.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        menuRoot.GetComponent<Image>().color = new Color(.035f,.045f,.09f,.96f);
        var title = MakeText(menuRoot.transform, "Title", "STRIKE ZONE", 42, TextAnchor.MiddleCenter, font);
        title.rectTransform.anchorMin = new Vector2(.1f,.55f); title.rectTransform.anchorMax = new Vector2(.9f,.8f);
        title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
        var subtitle = MakeText(menuRoot.transform, "Subtitle", "MOBILE  •  OFFLINE TRAINING", 18, TextAnchor.MiddleCenter, font);
        subtitle.rectTransform.anchorMin = new Vector2(.1f,.46f); subtitle.rectTransform.anchorMax = new Vector2(.9f,.57f);
        subtitle.rectTransform.offsetMin = subtitle.rectTransform.offsetMax = Vector2.zero;
        var play = MakeButton(menuRoot.transform, "PLAY  /  TRAINING", new Vector2(.5f,.35f), font, () => SetPlaying(true));
        play.GetComponent<RectTransform>().sizeDelta = new Vector2(300,70);
    }

    Text MakeText(Transform parent, string n, string value, int size, TextAnchor anchor, Font font)
    {
        var obj = new GameObject(n, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        var t = obj.GetComponent<Text>();
        t.text = value; t.font = font; t.fontSize = size; t.alignment = anchor; t.color = Color.white;
        return t;
    }

    Button MakeButton(Transform parent, string label, Vector2 anchor, Font font, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor; rt.anchoredPosition = Vector2.zero;
        obj.GetComponent<Image>().color = new Color(.20f,.28f,.55f,.98f);
        var button = obj.GetComponent<Button>(); button.onClick.AddListener(action);
        var text = MakeText(obj.transform, "Label", label, 18, TextAnchor.MiddleCenter, font);
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    void MakeTouchButton(Transform parent, string label, Vector2 anchor, Vector2 offset, UnityEngine.Events.UnityAction action)
    {
        var b = MakeButton(parent, label, anchor, Resources.GetBuiltinResource<Font>("Arial.ttf"), action);
        var rt = b.GetComponent<RectTransform>();
        rt.anchoredPosition = offset; rt.sizeDelta = new Vector2(88,58);
    }

    void SetPlaying(bool value)
    {
        started = value;
        if (menuRoot) menuRoot.SetActive(!value);
        Cursor.lockState = value && !Application.isMobilePlatform ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }

    void Update()
    {
        if (!started) return;
        fireCooldown -= Time.deltaTime;
        if (Application.isMobilePlatform)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SetPlaying(false);
        }
        else
        {
            yaw += Input.GetAxis("Mouse X") * 2.3f;
            pitch -= Input.GetAxis("Mouse Y") * 2.0f;
            pitch = Mathf.Clamp(pitch, -75, 75);
            if (Input.GetMouseButton(0)) Shoot();
            if (Input.GetKeyDown(KeyCode.R)) Reload();
        }
        player.rotation = Quaternion.Euler(0, yaw, 0);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = (player.right * h + player.forward * v).normalized;
        controller.Move((dir * 5.2f + Vector3.down * 2f) * Time.deltaTime);
        if (hudText) hudText.text = "STRIKE ZONE     HP " + playerHealth + "     " + ammo + " / " + reserveAmmo + "     KILLS " + kills;
    }

    void MoveForward(float amount)
    {
        if (started && controller) controller.Move(player.forward * amount);
    }

    void Shoot()
    {
        if (!started || fireCooldown > 0 || ammo <= 0) return;
        fireCooldown = .15f; ammo--;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 90f))
        {
            var bot = hit.collider.GetComponent<BotAgent>();
            if (bot) bot.TakeDamage(34);
        }
    }

    void Reload()
    {
        int needed = 30 - ammo;
        int load = Mathf.Min(needed, reserveAmmo);
        ammo += load; reserveAmmo -= load;
    }

    public void BotKilled(BotAgent bot)
    {
        kills++;
        bot.Respawn();
    }

    public Transform PlayerTransform { get { return player; } }
}

public class BotAgent : MonoBehaviour
{
    private int health = 100;
    private Vector3 home;
    private float timer;
    void Start() { home = transform.position; }
    void Update()
    {
        if (GameBootstrap.Instance == null || GameBootstrap.Instance.PlayerTransform == null) return;
        var target = GameBootstrap.Instance.PlayerTransform.position;
        Vector3 flat = target - transform.position; flat.y = 0;
        if (flat.magnitude < 13f && flat.magnitude > 2.4f)
            transform.position += flat.normalized * (1.1f * Time.deltaTime);
        if (flat.sqrMagnitude > .1f) transform.rotation = Quaternion.LookRotation(flat);
        timer += Time.deltaTime;
    }
    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0 && GameBootstrap.Instance != null) GameBootstrap.Instance.BotKilled(this);
    }
    public void Respawn()
    {
        health = 100;
        transform.position = home;
    }
}
