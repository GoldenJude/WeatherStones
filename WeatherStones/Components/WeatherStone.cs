using System;
using UnityEngine;

namespace WeatherStones.Components
{
    public class WeatherStone : MonoBehaviour, Interactable, TextReceiver
    {
        private ZNetView znv;
        private EnvZone env;
        private MeshRenderer renderer;

        public float speed = 1;
        public float amplitude = 1;
        private Color col = Color.white;

        private readonly bool defaultEnabled = true;
        private readonly string zdo_enabled = "enabled";
        private readonly string zdo_env = "env";

        private bool Valid => znv != null && znv.IsValid();

        private void Awake()
        {
            znv = GetComponent<ZNetView>();

            if (!Valid) return;

            env = GetComponentInChildren<EnvZone>(true);
            env.m_force = false; //cant be fucked opening the project and recompiling bundle

            renderer = GetComponentInChildren<MeshRenderer>(true);

            znv.Register(nameof(RPC_SetEnv), new Action<long, string>(RPC_SetEnv));
            znv.Register(nameof(RPC_UpdateStone), new Action<long>(RPC_UpdateStone));

            this.znv.InvokeRPC(nameof(RPC_UpdateStone));
        }

        private void FixedUpdate()
        {
            renderer?.material.SetColor("_EmissionColor", col * ((Mathf.Sin(Time.time * speed) * amplitude) + 0.5f));
        }

        public void RPC_UpdateStone(long sender)
        {
            UpdateColor();
            UpdateEnv();
            GetComponentInChildren<SphereCollider>(true).radius = Main.radius.Value;
        }

        private void UpdateEnv()
        {
            if (!Valid) return;

            env.m_environment = GetText();
        }

        private void UpdateColor()
        {
            if (!Valid) return;

            if (!znv.GetZDO().GetBool(zdo_enabled, defaultEnabled)) col = Color.black;

            var envSetup = EnvMan.instance?.GetEnv(GetText());
            if (envSetup != null)
            {
                col = envSetup.m_ambColorDay;
            }
            else col = Color.black;

            renderer.material.SetColor("_EmissionColor", col);
        }

        public void ToggleActive(bool status)
        {
            if (!Valid) return;

            znv.GetZDO().Set(zdo_enabled, status);
            znv.InvokeRPC(nameof(RPC_UpdateStone));
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold) return false;

            if(!PrivateArea.CheckAccess(transform.position, 0f, true, false))
            {
                user.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
                return true;
            }
            TextInput.instance.RequestText(this, "$WS_stone_big", 20);

            return true;
        }

        public void SetText(string text)
        {
            if (!Valid) return;

            this.znv.InvokeRPC(nameof(RPC_SetEnv), new object[] { text });
        }

        private void RPC_SetEnv(long sender, string envName)
        {
            if (!Valid) return;

            if (!this.znv.IsOwner()) return;

            if (envName == string.Empty) ToggleActive(false);

            this.znv.GetZDO().Set(zdo_env, envName);

            znv.InvokeRPC(nameof(RPC_UpdateStone));
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetText()
        {
            if (!Valid) return string.Empty;

            return znv.GetZDO().GetString(zdo_env, string.Empty);
        }
    }
}
