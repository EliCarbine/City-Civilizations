using System;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.UI;
using Unity.Entities;

namespace CitizenSoul
{
    /// <summary>
    /// M4 (F58): the soul inspector panel. While the player follows a citizen, this
    /// system projects the followed soul (mood, mind, ties, skills, recent diary) into
    /// UI bindings that a panel renders beside the vanilla Lifepath/Followed readout.
    ///
    /// Surface note: binding classes here are modeled from public CS2 UI-modding
    /// documentation; real-assembly verification happens at M6 QA. All work is
    /// throttled, try/caught, and a no-op until a followed soul exists.
    /// </summary>
    public partial class SoulPanelUISystem : UISystemBase
    {
        private const int RefreshFrames = 30;   // ~half a second at 60 fps

        private static readonly SoulInspector.View EmptyView = new SoulInspector.View
        {
            Summary = SoulLocale.Get("PANEL_EMPTY"),
            Details = "",
            Skills = "",
            Recent = ""
        };

        private EntityQuery m_Followed;
        private ValueBinding<string> m_Summary;
        private ValueBinding<string> m_Details;
        private ValueBinding<string> m_Skills;
        private ValueBinding<string> m_Recent;

        private int m_Tick;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Followed = GetEntityQuery(ComponentType.ReadOnly<Followed>());
            AddBinding(m_Summary = new ValueBinding<string>("soulpanel", "summary", ""));
            AddBinding(m_Details = new ValueBinding<string>("soulpanel", "details", ""));
            AddBinding(m_Skills = new ValueBinding<string>("soulpanel", "skills", ""));
            AddBinding(m_Recent = new ValueBinding<string>("soulpanel", "recent", ""));
        }

        protected override void OnUpdate()
        {
            if (!SoulOptions.IsOn("ui")) return; // M6: options.ini gate
            try
            {
                m_Tick++;
                if (m_Tick % RefreshFrames != 0) return;
                Refresh();
            }
            catch (Exception ex)
            {
                SoulLog.Exception("panel.update", ex);
            }
        }

        private void Refresh()
        {
            if (m_Followed.CalculateEntityCount() != 1)
            {
                Send(EmptyView);
                return;
            }

            Entity e = m_Followed.GetSingletonEntity();
            EntityManager em = EntityManager;
            if (!em.HasComponent<SoulMeta>(e))
            {
                Send(EmptyView);
                return;
            }

            Send(SoulInspector.Build(em, e));
        }

        private void Send(SoulInspector.View v)
        {
            m_Summary.Update(v.Summary ?? "");
            m_Details.Update(v.Details ?? "");
            m_Skills.Update(v.Skills ?? "");
            m_Recent.Update(v.Recent ?? "");
        }
    }
}
