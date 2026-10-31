using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPGestureReceiver : MonoBehaviour
{
    [Header("UDP Settings")]
    public int port = 5005;

    [Header("Gesture Timeouts")]
    public float runTimeout = 0.3f;
    public float walkTimeout = 0.3f;
    public float turnTimeout = 0.15f;
    public float moveTimeout = 0.2f;

    [Header("Interaction Settings")]
    public float interactCooldown = 1.5f;
    private float lastInteractTime = -100f;

    [Header("Action Cooldowns")]
    public float grabCooldown = 1.5f;
    public float uvCooldown = 0.5f;
    public float slotCooldown = 0.5f;

    private float lastRunTime = -1f;
    private float lastWalkTime = -1f;
    private float lastTurnLeftTime = -1f;
    private float lastTurnRightTime = -1f;
    private float lastMoveTime = -1f;

    private float lastGrabTime = -100f;
    private float lastUvTime = -100f;
    private float lastSlotTime = -100f;

    [Header("Pickup Settings")]
    public float pickupRange = 5f;
    public Camera playerCamera;
    public float sphereCastRadius = 0.4f;

    [Header("Crosshair Settings")]
    public UnityEngine.UI.Image crosshairUI;

    public Color defaultColor = new Color(1f, 1f, 1f, 0.5f);
    public Color hoverColor = Color.red;

    public Vector2 defaultSize = new Vector2(6f, 6f);
    public Vector2 hoverSize = new Vector2(12f, 12f);

    [Header("Flashlight Switching")]
    public GameObject flashlight_;
    public GameObject flashlight_uv_copy;

    public FlashlightBattery flashlightBattery;

    [Header("Inventory")]
    public backpack playerBackpack;
    public int currentSlot = 1;

    [Header("Whack-A-Mole")]
    public WhackAMoleManager moleManager;

    // Generator mini-game: setting this notifies Python (GENERATOR_ON / GENERATOR_OFF)
    private bool _generatorActive = false;

    private FirstPersonMovement movement;
    [Header("Turkey King QTE")]
    public TurkeyKingQTE turkeyKingQTE;

    public bool generatorActive
    {
        get { return _generatorActive; }

        set
        {
            if (_generatorActive == value)
                return;

            _generatorActive = value;

            SendGeneratorModeToPython(_generatorActive);
        }
    }

    public string latestGesture = "";

    private UdpClient udpClient;
    private Thread receiveThread;

    // Python's IP + temporary source port (learned from incoming packets)
    private IPEndPoint pythonEndpoint;

    // Message queue (no gestures get dropped)
    private readonly Queue<string> messageQueue = new Queue<string>();
    private readonly object lockObj = new object();

    private float turnSpeedFactor = 0f;



    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        movement = GetComponent<FirstPersonMovement>();

        try
        {
            udpClient = new UdpClient(port);

            receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true
            };

            receiveThread.Start();

            Debug.Log("UDP Gesture Receiver started on port " + port);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start UDP receiver: " + e.Message);
        }
    }


    // =========================================================
    // DESTROY
    // =========================================================

    void OnDestroy()
    {
        try
        {
            // Tell Python to return to normal LOOK mode
            if (_generatorActive)
            {
                SendGeneratorModeToPython(false);
            }
        }
        catch { }

        try { receiveThread?.Abort(); } catch { }

        try { udpClient?.Close(); } catch { }
    }


    // =========================================================
    // SEND GENERATOR MODE TO PYTHON
    // =========================================================

    private void SendGeneratorModeToPython(bool active)
    {
        lock (lockObj)
        {
            if (pythonEndpoint == null)
            {
                Debug.LogError(
                    "❌ PYTHON ENDPOINT IS NULL - CANNOT SEND GENERATOR MODE"
                );
                return;
            }

            if (udpClient == null)
            {
                Debug.LogWarning(
                    "Cannot send generator mode: UDP client is null."
                );
                return;
            }

            string message = active ? "GENERATOR_ON" : "GENERATOR_OFF";

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);

                udpClient.Send(data, data.Length, pythonEndpoint);

                Debug.Log("UDP → Python: " + message);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "Failed to send generator mode: " + e.Message
                );
            }
        }
    }


    // =========================================================
    // UDP RECEIVE
    // =========================================================

    void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);

                string msg = Encoding.UTF8.GetString(data).Trim();

                lock (lockObj)
                {
                    // Remember Python's IP and temporary port so Unity
                    // can send GENERATOR_ON / GENERATOR_OFF back.
                    pythonEndpoint =
                        new IPEndPoint(remoteEP.Address, remoteEP.Port);

                    messageQueue.Enqueue(msg);
                }
            }
            catch (Exception e)
            {
                if (!(e is ThreadAbortException))
                {
                    Debug.LogWarning("UDP receive error: " + e.Message);
                }

                break;
            }
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        UpdateCrosshair();


        // =====================================================
        // MOVEMENT GESTURE STATUS
        // =====================================================

        if (movement != null)
        {
            movement.gestureRunning =
                (Time.time - lastRunTime) < runTimeout;

            movement.gestureWalking =
                (Time.time - lastWalkTime) < walkTimeout;

            movement.gestureTurnLeft =
                (Time.time - lastTurnLeftTime) < turnTimeout;

            movement.gestureTurnRight =
                (Time.time - lastTurnRightTime) < turnTimeout;

            movement.gestureTurnFactor = turnSpeedFactor;

            // Movement timeout protection
            if (Time.time - lastMoveTime > moveTimeout)
            {
                movement.forwardInput = 0f;
                movement.rightInput = 0f;
            }
        }


        // =====================================================
        // PROCESS UDP MESSAGES
        // =====================================================

        while (true)
        {
            string msg;

            lock (lockObj)
            {
                if (messageQueue.Count == 0)
                    break;

                msg = messageQueue.Dequeue();
            }

            bool qteWasActive =
                turkeyKingQTE != null && turkeyKingQTE.IsQTEActive;

            if (turkeyKingQTE != null &&
                turkeyKingQTE.enabled &&
                turkeyKingQTE.gameObject.activeInHierarchy)
            {
                turkeyKingQTE.ReceiveGesture(msg);
            }

            if (qteWasActive)
            {
                continue;
            }

            // =================================================
            // GENERATOR MINI-GAME
            // Open palm from Python = "PLAY". Everything else ignored.
            // =================================================

            if (generatorActive)
            {
                if (msg == "PLAY")
                {
                    Debug.Log(">>> GENERATOR PLAY ACCEPTED <<<");

                    latestGesture = "PLAY";
                }

                continue;
            }


            // =================================================
            // MOVEMENT
            // =================================================

            if (movement == null)
                continue;


            if (msg.StartsWith("MOVE F:"))
            {
                ParseMoveMessage(msg);
            }

            if (msg.Contains("RUN"))
            {
                lastRunTime = Time.time;
            }

            if (msg.Contains("WALK"))
            {
                lastWalkTime = Time.time;
            }

            if (msg.Contains("TURN LEFT"))
            {
                lastTurnLeftTime = Time.time;
                turnSpeedFactor = ParseFactor(msg);
            }

            if (msg.Contains("TURN RIGHT"))
            {
                lastTurnRightTime = Time.time;
                turnSpeedFactor = ParseFactor(msg);
            }

            if (msg == "TURN STOP")
            {
                turnSpeedFactor = 0f;
            }


            // =================================================
            // GRAB
            // =================================================

            if (msg.Contains("GRAB") &&
                Time.time - lastGrabTime > grabCooldown)
            {
                lastGrabTime = Time.time;

                TryInteract();
            }


            // =================================================
            // INVENTORY SLOTS / WHACK-A-MOLE
            // =================================================

            if (msg == "1") HandleSlotInput(1);
            if (msg == "2") HandleSlotInput(2);
            if (msg == "3") HandleSlotInput(3);
            if (msg == "4") HandleSlotInput(4);

            if (msg.StartsWith("SLOT "))
            {
                if (int.TryParse(msg.Substring(5), out int slot))
                {
                    HandleSlotInput(slot);
                }
            }
        }
    }


    // =========================================================
    // SLOT INPUT (whack-a-mole hit OR inventory select)
    // =========================================================

    void HandleSlotInput(int slot)
    {
        if (moleManager != null &&
            moleManager.isGameActive &&
            slot >= 1 && slot <= 4)
        {
            Debug.Log("打地鼠模式 → HammerHit(" + slot + ")");
            moleManager.HammerHit(slot);
        }
        else
        {
            SelectSlot(slot);
        }
    }


    // =========================================================
    // MOVEMENT PARSING
    // =========================================================

    void ParseMoveMessage(string msg)
    {
        try
        {
            string[] parts = msg.Split(' ');

            float f = 0f;
            float r = 0f;

            foreach (string part in parts)
            {
                if (part.StartsWith("F:"))
                {
                    float.TryParse(
                        part.Substring(2),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out f
                    );
                }
                else if (part.StartsWith("R:"))
                {
                    float.TryParse(
                        part.Substring(2),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out r
                    );
                }
            }

            movement.forwardInput = f;
            movement.rightInput = r;

            lastMoveTime = Time.time;
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "Failed to parse MOVE message: " + msg + " | " + e.Message
            );
        }
    }

    float ParseFactor(string msg)
    {
        int idx = msg.IndexOf(':');

        if (idx >= 0 &&
            float.TryParse(
                msg.Substring(idx + 1),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float v))
        {
            return Mathf.Clamp(v, 0f, 1f);
        }

        return 1f;
    }


    // =========================================================
    // AIM HELPER: Raycast first, SphereCast around it as fallback
    //
    // 1) The thin Raycast wins when it hits something directly
    //    (precise aiming, avoids grabbing the wrong object).
    // 2) If the ray misses, a SphereCast along the same line
    //    catches objects near the crosshair (forgiving aim).
    // Crosshair and TryInteract both use this, so what turns
    // red is exactly what GRAB will interact with.
    // =========================================================


    bool GetAimHit(out RaycastHit hit)
    {
        hit = default(RaycastHit);

        if (playerCamera == null)
            return false;

        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        // 先用精準射線偵測
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        // 由近到遠檢查，略過冷凍庫進出觸發區
        foreach (RaycastHit h in hits)
        {
            if (h.collider.GetComponentInParent<FreezerTrigger>() != null)
                continue;

            hit = h;
            return true;
        }

        // 精準射線沒找到有效目標，再用球形射線輔助
        RaycastHit[] sphereHits = Physics.SphereCastAll(
            origin,
            sphereCastRadius,
            direction,
            pickupRange,
            ~0,
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(
            sphereHits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit h in sphereHits)
        {
            if (h.collider.GetComponentInParent<FreezerTrigger>() != null)
                continue;

            hit = h;
            return true;
        }

        return false;
    }



    bool IsInteractable(RaycastHit hit)
    {
        Collider col = hit.collider;

        if (col.CompareTag("Pickable") ||
            col.CompareTag("Interactable") ||
            col.CompareTag("MoleMachine") ||
            col.CompareTag("Mole"))
            return true;

        if (col.GetComponentInParent<RotatingDoor>() != null) return true;
        if (col.GetComponentInParent<lever_controller>() != null) return true;
        if (col.GetComponentInParent<GateLeverController>() != null) return true;
        if (col.GetComponentInParent<FuseBoxController>() != null) return true;
        if (col.GetComponentInParent<GeneratorInteract>() != null) return true;
        if (col.GetComponentInParent<SlideMovement>() != null) return true;

        string n = col.name.ToLower();
        if (n.Contains("slide") || n.Contains("tunnel")) return true;

        return false;
    }


    // =========================================================
    // CROSSHAIR
    // =========================================================

    void UpdateCrosshair()
    {
        if (crosshairUI == null || playerCamera == null)
            return;

        bool isLookingAtInteractable =
            GetAimHit(out RaycastHit hit) && IsInteractable(hit);

        if (isLookingAtInteractable)
        {
            crosshairUI.color = hoverColor;
            crosshairUI.rectTransform.sizeDelta = hoverSize;
        }
        else
        {
            crosshairUI.color = defaultColor;
            crosshairUI.rectTransform.sizeDelta = defaultSize;
        }
    }


    // =========================================================
    // INTERACTION / PICKUP
    // =========================================================

    void TryInteract()
    {
        if (Time.time - lastInteractTime < interactCooldown)
            return;

        // =========================================
        // 0. Use battery from backpack
        // (only when battery is the selected item)
        // =========================================
        if (playerBackpack != null &&
            playerBackpack.GetSelectedItem() == "battery")
        {
            if (playerBackpack.GetItemCount("battery") <= 0)
            {
                Debug.Log("背包沒有電池！");
                return;
            }

            if (flashlightBattery == null)
            {
                Debug.LogWarning("尚未指定 FlashlightBattery 元件！");
                return;
            }

            if (flashlightBattery.currentBattery >=
                flashlightBattery.maxBattery)
            {
                Debug.Log("白光手電筒電量已滿，不消耗電池！");
                return;
            }

            // Confirm recharge succeeded first, then consume a battery
            if (flashlightBattery.TryAddBattery())
            {
                playerBackpack.UseItemByName("battery");
                lastInteractTime = Time.time;
            }

            return;
        }

        // =========================================
        // 1. Check camera + aim
        // =========================================
        if (playerCamera == null)
        {
            Debug.LogWarning("playerCamera not assigned.");
            return;
        }

        Debug.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * pickupRange,
            Color.red,
            2f
        );

        if (!GetAimHit(out RaycastHit hit))
        {
            Debug.Log("揮空了，眼前沒東西。");
            return;
        }

        // =========================================
        // 2. GENERATOR
        // =========================================
        GeneratorInteract generator =
            hit.collider.GetComponentInParent<GeneratorInteract>();

        if (generator != null)
        {
            Debug.Log("GRAB → GENERATOR");

            latestGesture = "";
            generatorActive = true;   // sends GENERATOR_ON to Python

            Debug.Log(">>> UNITY GENERATOR MODE ON <<<");

            generator.Activate();

            lastInteractTime = Time.time;
            return;
        }

        // =========================================
        // 3. WHACK-A-MOLE MACHINE
        // =========================================
        if (hit.collider.CompareTag("MoleMachine"))
        {
            if (moleManager != null && !moleManager.isGameActive)
            {
                if (playerBackpack == null)
                {
                    Debug.LogWarning("沒有指定 Player Backpack！");
                    return;
                }

                if (playerBackpack.GetSelectedItem() != "coin")
                {
                    Debug.Log("請先在背包選擇 coin！");
                    return;
                }

                if (playerBackpack.GetSelectedItemCount() <= 0)
                {
                    Debug.Log("沒有 coin，無法開始打地鼠！");
                    return;
                }

                if (!playerBackpack.UseSelectedItem())
                {
                    Debug.Log("硬幣使用失敗！");
                    return;
                }

                Debug.Log("已使用 1 枚 coin，開始打地鼠遊戲！");
                moleManager.StartGame();
            }

            lastInteractTime = Time.time;
            return;
        }

        // =========================================
        // 4. TURKEY MOLE
        // =========================================
        if (hit.collider.CompareTag("Mole"))
        {
            TurkeyMole mole =
                hit.collider.GetComponentInParent<TurkeyMole>();

            if (mole != null)
            {
                Debug.Log("比讚打中火雞！");
                mole.OnHitByPlayer();

                lastInteractTime = Time.time;
                return;
            }
        }

        // =========================================
        // 5. FREEZER DOOR
        // =========================================
        RotatingDoor door =
            hit.collider.GetComponentInParent<RotatingDoor>();

        if (door != null)
        {
            Debug.Log("比讚 → 冷凍庫門");
            door.OpenFromGesture();

            lastInteractTime = Time.time;
            return;
        }

        // =========================================
        // 6. FUSE BOX
        // =========================================
        FuseBoxController fuseBox =
            hit.collider.GetComponentInParent<FuseBoxController>();

        if (fuseBox != null)
        {
            Debug.Log("比讚 → 電箱");
            fuseBox.ActivateFuseBox();

            lastInteractTime = Time.time;
            return;
        }

        // =========================================
        // 7. FREEZER LEVER
        // =========================================
        lever_controller lever =
            hit.collider.GetComponent<lever_controller>();

        if (lever == null)
        {
            lever = hit.collider.GetComponentInParent<lever_controller>();
        }

        if (lever != null)
        {
            Debug.Log("操作冷凍庫拉桿：" + lever.gameObject.name);
            lever.ToggleLever();

            lastInteractTime = Time.time;
            return;
        }

        // =========================================
        // 8. GATE LEVER
        // =========================================
        GateLeverController gateLever =
            hit.collider.GetComponent<GateLeverController>();

        if (gateLever == null)
        {
            gateLever =
                hit.collider.GetComponentInParent<GateLeverController>();
        }

        if (gateLever != null)
        {
            Debug.Log("操作大門拉桿：" + gateLever.gameObject.name);
            gateLever.ToggleLever();

            lastInteractTime = Time.time;
            return;
        }

        // =========================================
        // 9. SLIDE
        // =========================================
        SlideMovement slide =
            hit.collider.GetComponentInParent<SlideMovement>();

        if (slide != null)
        {
            slide.TryStartSlide(transform.root);

            lastInteractTime = Time.time;
            return;
        }

        // Hit object is a slide/tunnel model outside the script's hierarchy:
        // start whichever slide entrance is nearest to the player.
        string n = hit.collider.name.ToLower();

        if (n.Contains("slide") || n.Contains("tunnel"))
        {
            Debug.Log("GRAB → nearest slide, hit: " + hit.collider.name);
            SlideMovement.TryStartNearest(transform.root);

            lastInteractTime = Time.time;
            return;
        }

        // =========================================
        // 10. PICKUP ITEM
        // =========================================
        if (hit.collider.CompareTag("Pickable"))
        {
            backpackitem item =
                hit.collider.GetComponentInParent<backpackitem>();

            if (item == null)
            {
                Debug.LogWarning(
                    "此 Pickable 物件沒有 backpackitem：" +
                    hit.collider.gameObject.name
                );
                return;
            }

            if (playerBackpack == null)
            {
                Debug.LogWarning("Player Backpack is not assigned!");
                return;
            }

            Debug.Log("準備加入背包：" + item.itemName);

            bool added = playerBackpack.AddItem(
                item.itemName,
                item.itemIcon
            );

            if (added)
            {
                Debug.Log("撿起物品：" + item.itemName);

                // Picking up the UV flashlight swaps the flashlight model
                if (item.itemName == "flashlight")
                {
                    if (flashlight_ != null)
                        flashlight_.SetActive(false);

                    if (flashlight_uv_copy != null)
                        flashlight_uv_copy.SetActive(true);

                    playerBackpack.SelectSlot(1);

                    Debug.Log("已切換到紫光手電筒！");
                }

                Destroy(item.gameObject);
                lastInteractTime = Time.time;
            }
            else
            {
                Debug.Log("背包已滿，無法撿取：" + item.itemName);
            }

            return;
        }

        // =========================================
        // 11. OTHER
        // =========================================
        Debug.Log("看到東西，但不能互動：" + hit.collider.gameObject.name);
    }


    // =========================================================
    // INVENTORY SLOT
    // =========================================================

    void SelectSlot(int slot)
    {
        currentSlot = slot;

        if (playerBackpack != null)
        {
            playerBackpack.SelectSlot(slot);
        }

        Debug.Log("Inventory slot selected: " + slot);
    }
}