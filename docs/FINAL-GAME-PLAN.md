# Cut Free Pets — COMPLETE PROJECT REQUIREMENTS & GAME BIBLE

> **Working title:** Cut Free Pets (final store name later)  
> **Document type:** Full requirements + design + tech + data + auth + privacy + build bible  
> **Audience:** Solo developer  
> **Version:** 3.0 — Complete (Stars REMOVED · Attempts REMOVED · Google login + user data ADDED)  
> **Rule:** This file is the single source of truth. If it is not here, it is not in scope until added.

---

## Important locked removals

| System | Status |
|--------|--------|
| Stars / star unlock / best stars | **REMOVED** |
| Attempts / lives / continue-for-lives | **REMOVED** |
| Dog unlock | Clear **Level 10** (tunable), not stars |

---

## Table of Contents

### A. Product
1. Game Identity  
2. Goals & Success Criteria  
3. Locked Decisions  
4. In Scope / Out of Scope  
5. Functional Requirements (Master List)  
6. Non-Functional Requirements  

### B. Game Design
7. How The Game Works  
8. Cut System  
9. Rope & Chain System  
10. Animal Behavior  
11. Progress & Dog Unlock (No Stars)  
12. Fail / Oops / Restart (No Attempts)  
13. Hint System  
14. Ads System  

### C. User, Account & Data
15. Player Modes (Guest vs Google)  
16. Google Sign-In Requirements  
17. User Data Model (Local + Cloud)  
18. Sync / Merge Rules  
19. Privacy, Consent & Legal  
20. Analytics & Events  
21. Permissions  

### D. UX / Screens
22. Full User Journeys  
23. Screen Map & Navigation  
24. Screen Specs (Including Account)  

### E. Content
25. Level Design Spec  

### F. Technical
26. Tech Stack  
27. Backend / Services  
28. Project Folder Structure  
29. Code Architecture  
30. Scripts Responsibility  
31. Level Data Format  
32. Scenes & Prefabs  
33. Animation / Audio / Art  
34. Camera, Input, UI, Performance Rules  

### G. Delivery
35. Build Order  
36. Testing & Acceptance  
37. Play Store Requirements  
38. Marketing Ads Spec  
39. Risks & Edge Cases  
40. Later Roadmap  
41. Glossary  
42. One-Page Summary  

---

# A. PRODUCT

## 1. Game Identity

### 1.1 One sentence
Modern casual **3D puzzle**: swipe to cut ropes/chains → short scissors snip → free a cute animal.

### 1.2 Genre / feel
Casual puzzle with dopamine of Amaze GO / Mob Control ads, rope feel of Cut the Rope.  
Fun = snip + free animal, **not** points.

### 1.3 Platform
| Item | Requirement |
|------|-------------|
| Primary | Android (Google Play) |
| Secondary later | iOS |
| Orientation | Portrait |
| Language | English UI only (v1) |
| Offline | Core gameplay works offline; login/ads/cloud need network |

### 1.4 Monetization
Free + ads (Hint rewarded + interstitial every 3–4 wins).  
No paywall to play core levels in v1.

---

## 2. Goals & Success Criteria

### 2.1 Product goals (priority order)
1. Player enjoys snip + free moment  
2. Understands in &lt; 3 seconds  
3. Wants “one more level”  
4. Downloads grow  
5. Fair ads revenue  

### 2.2 Acceptance “fun gate”
Ask 5 testers after 2 minutes:
- Understood without help?  
- Snip feels good?  
- Cute/fun?  
- Want next?  

Need mostly yes before mass levels/ads polish.

### 2.3 Soft launch quality gates
- Crash-free session rate high  
- No star/attempt UI anywhere  
- Progress saves correctly guest + Google  
- Ads do not show on Oops/Restart  

---

## 3. Locked Decisions

| Topic | Decision |
|-------|----------|
| Visuals | Stylized **3D** URP |
| Rope | Cut-the-Rope feel |
| Cut | Swipe + scissors ~0.2s |
| Animals | Cat → Dog → third later |
| Levels v1 | 20–30 |
| Stars | None |
| Attempts | None |
| Dog unlock | Clear Level 10 |
| Account | **Guest play + optional Google Sign-In** |
| Cloud save | Yes when Google linked (see §15–18) |
| Budget start | $0 assets; Play Console $25 when publishing |
| Name | Later |

---

## 4. In Scope / Out of Scope

### 4.1 In scope (v1 / v1.1)
- Full casual cut gameplay  
- 20–30 levels, ropes + chains  
- Guest local progress  
- Optional Google login + cloud progress sync  
- Hint ads + win interstitials  
- Settings, privacy links, basic analytics  
- Android release  

### 4.2 Out of scope (v1)
- Stars / lives systems  
- Forced login before play  
- Facebook/Apple login (Apple later with iOS)  
- Multiplayer / chat / friends list  
- Spinner  
- Zoo decorate meta  
- iOS (until Android stable)  
- Real-money gambling mechanics  

---

## 5. Functional Requirements (Master List)

### FR-GAME
| ID | Requirement |
|----|-------------|
| FR-G01 | Player can swipe to cut a rope/chain |
| FR-G02 | Scissors FX ~0.2s plays on valid cut |
| FR-G03 | Rope splits and falls (not instant delete) |
| FR-G04 | Clearing all MustCut bindings wins level |
| FR-G05 | Wrong-order cut shows Oops and resets ropes |
| FR-G06 | Unlimited retries (no attempts) |
| FR-G07 | Restart reloads level without ad |
| FR-G08 | Hint via rewarded ad highlights next rope |
| FR-G09 | Win shows celebration + Next (no stars) |
| FR-G10 | Level 1 shows swipe tutorial hand once |
| FR-G11 | Cat chapter starts available |
| FR-G12 | Clearing Level 10 unlocks Dog chapter |
| FR-G13 | Chains appear in later levels with clank SFX |
| FR-G14 | Gameplay works offline |

### FR-PROGRESS
| ID | Requirement |
|----|-------------|
| FR-P01 | Level 1 unlocked by default |
| FR-P02 | Clearing level K unlocks K+1 |
| FR-P03 | Cleared levels show checkmark (not stars) |
| FR-P04 | Progress saved on device for guest |
| FR-P05 | Progress syncs to cloud when Google signed in |
| FR-P06 | Replaying cleared levels always allowed free |

### FR-ADS
| ID | Requirement |
|----|-------------|
| FR-A01 | No interstitial on first ~8 wins |
| FR-A02 | Interstitial every 3–4 wins after protection |
| FR-A03 | Never interstitial on Oops/Restart/fail |
| FR-A04 | Hint uses rewarded ad |
| FR-A05 | No banner during gameplay |
| FR-A06 | Test ad IDs in debug; real IDs in release |

### FR-ACCOUNT
| ID | Requirement |
|----|-------------|
| FR-U01 | User can play as Guest without login |
| FR-U02 | User can Sign in with Google from Home/Settings |
| FR-U03 | After Google login, progress can sync to cloud |
| FR-U04 | User can Sign out (guest local remains policy below) |
| FR-U05 | User can request account/data delete (Play policy) |
| FR-U06 | Login is never required to start Level 1 |
| FR-U07 | Show signed-in Google account name/email (masked ok) |

### FR-SETTINGS
| ID | Requirement |
|----|-------------|
| FR-S01 | Music on/off |
| FR-S02 | Sound on/off |
| FR-S03 | Haptics on/off optional |
| FR-S04 | Open Privacy Policy |
| FR-S05 | Open Terms (if used) |
| FR-S06 | Account section: Sign in / Sign out / Delete |

---

## 6. Non-Functional Requirements

| ID | Area | Requirement |
|----|------|-------------|
| NFR-01 | Performance | Stable play on mid Android; avoid heavy lag |
| NFR-02 | Startup | Reach Home quickly; Splash ≤ ~2s perceived |
| NFR-03 | Reliability | Progress must not randomly reset |
| NFR-04 | Security | Do not store Google password; use OAuth tokens via SDK |
| NFR-05 | Privacy | Collect minimum data; disclose in policy + Data Safety |
| NFR-06 | Accessibility | Large buttons; readable ropes; English clear labels |
| NFR-07 | Localization | English only v1; structure text for later |
| NFR-08 | Maintainability | Clear folders/scripts; no god-class |
| NFR-09 | Network | Graceful offline: gameplay OK; login/sync/ads fail softly |
| NFR-10 | Versioning | save_version field for migrations |

---

# B. GAME DESIGN

## 7. How The Game Works

```
Load level → animal + bindings
→ swipe cut
→ valid: scissors + split
→ invalid order: Oops + reset (unlimited)
→ all MustCut cut → WIN → Next
```

Win condition: all MustCut bindings cut.  
Level length target: 15–40 seconds.

Chapters:
- Cat from start  
- Dog after Level 10 cleared  
- Third later  

---

## 8. Cut System

### Valid cut timeline
Swipe trail → intersect → validate → scissors 0.2s + SFX → split → fall → cleanup.

### Invalid cut
Oops + shake + reset + no ad + no attempt cost.

### Tiny rules
- Min swipe distance  
- One cut at a time during scissors  
- First intersected rope only if multi-hit  
- Disable input on win celebration  

---

## 9. Rope & Chain System

Must: sway, cut at point, halves fall, synced sound.  
Stage A: tween fall. Stage B: physics/Verlet.  
Chain: heavier look + clank.

---

## 10. Animal Behavior

States: IdleTied, HappyFree, optional Oops flinch.  
Animator Idle + Happy trigger; fallback DOTween jump.

---

## 11. Progress & Dog Unlock (No Stars)

Track:
- `level_cleared[id]`  
- highest unlock  
- `dog_unlocked`  

Dog:
```
if level 10 cleared → dog_unlocked = true
```
UI: checkmarks, not stars.  
Win UI: no star row.

---

## 12. Fail / Oops / Restart (No Attempts)

No hearts, no attempt counter, no continue-lives ad.  
Oops = unlimited. Restart free.

---

## 13. Hint System

Hint → rewarded ad → glow correct rope 3–5s.  
No auto-cut. Soft fail if ad not ready.

---

## 14. Ads System

| Ad | Rule |
|----|------|
| Rewarded Hint | Optional |
| Interstitial | Every 3–4 wins after early protection |
| On Oops/Restart | Never |
| Banner gameplay | No |

Use AdMob. Never click own ads.

---

# C. USER, ACCOUNT & DATA

## 15. Player Modes (Guest vs Google)

### 15.1 Guest (default)
- No login required  
- Progress stored **locally on device**  
- Can play full game  
- Risk: uninstall/new phone may lose progress  

### 15.2 Google signed-in
- Optional  
- Progress stored **local + cloud**  
- Restore on new device after login  
- Better long-term retention  

### 15.3 Recommended UX policy (locked)
**Never force Google login before first play.**  
Show soft prompt after Level 5 or on Home:  
“Sign in with Google to keep your progress safe.”  
Dismissible.

---

## 16. Google Sign-In Requirements

### 16.1 Provider
- **Google Sign-In** on Android  
- Recommended stack for solo: **Firebase Authentication (Google provider)**  
- Alternative: Google Play Games Services login (optional later; different UX)

### 16.2 What Google gives the app
| Data | Use |
|------|-----|
| Google user id (uid / sub) | Primary account key |
| Email | Account display / support (optional show masked) |
| Display name | Optional greet |
| Profile photo URL | Optional avatar on Home |
| OAuth tokens | Handled by Firebase/SDK (do not custom-store passwords) |

### 16.3 What we never do
- Never ask for Google password in our UI  
- Never sell personal data  
- Never require login to cut ropes  

### 16.4 Auth flows

**Sign in**
```
User taps Sign in with Google
→ Google account picker
→ Success → Firebase Auth session
→ Create/load cloud profile
→ Merge with local progress (see §18)
→ UI shows signed-in state
```

**Sign out**
```
User taps Sign out
→ Clear auth session
→ Keep local progress copy on device
→ Cloud remains for next login
→ UI returns to Guest mode label
```

**Fail cases**
| Case | App behavior |
|------|----------------|
| No network | Toast: “Connect to internet to sign in” |
| User cancels picker | No change |
| Auth error | Soft error message; stay Guest |
| Play Services missing | Soft message / open update |

### 16.5 Account delete (required for stores when account exists)
```
Settings → Delete account
→ Confirm dialog (clear warning)
→ Delete cloud user data
→ Delete/sign out auth user
→ Reset or keep local guest? → Policy: reset cloud; offer keep local cleared copy as guest OR wipe — choose wipe cloud + keep local progress as guest unless user chooses “Delete all local too”
```

**Locked default delete policy:**
1. Delete cloud profile + auth  
2. Sign out  
3. Keep local device progress as Guest unless user checks “Also delete local progress”

---

## 17. User Data Model (Local + Cloud)

### 17.1 Local device save (always)
Technology: PlayerPrefs **or** JSON file in `persistentDataPath` (JSON preferred once cloud exists for easier merge).

**LocalProfile**
```text
save_version: int                  // current = 3
install_id: string                 // random UUID generated first launch
auth_uid: string | empty           // Google/Firebase uid if linked
is_guest: bool
display_name_cache: string
avatar_url_cache: string
email_cache: string                // optional; consider hashing/masking in logs

progress:
  highest_unlocked_level: int
  dog_unlocked: bool
  third_animal_unlocked: bool      // future
  levels: map<levelId, LevelProgressLocal>
    LevelProgressLocal:
      cleared: bool
      cleared_at_utc: string optional
      play_count: int optional
      last_played_at_utc: string optional

settings:
  music_on: bool
  sound_on: bool
  haptics_on: bool
  consented_privacy: bool
  consented_privacy_version: string
  analytics_opt_in: bool           // if you offer toggle where required

ads:
  lifetime_wins: int
  wins_since_interstitial: int

meta:
  first_open_utc: string
  last_open_utc: string
  app_version: string
  tutorial_level1_done: bool
```

### 17.2 Explicitly NOT stored
- Stars / mistakeCount for scoring  
- Attempts/lives  
- Google password  
- Full payment card data  
- Precise mid-rope cut state for resume (v1 restarts level)  

### 17.3 Cloud profile (when Google signed in)
Recommended: **Cloud Firestore** document  
Path: `users/{auth_uid}`

**CloudUserDoc**
```text
uid: string
email: string | null
display_name: string | null
photo_url: string | null
created_at: timestamp
updated_at: timestamp
last_device: string
app_version: string

progress:
  highest_unlocked_level: int
  dog_unlocked: bool
  third_animal_unlocked: bool
  levels: map or array of { id, cleared, cleared_at }
  // store only cleared ids to keep small if preferred

settings_sync_optional:
  music_on, sound_on, haptics_on   // optional; settings can stay device-only

schema_version: int
```

### 17.4 Server rules (Firestore)
- User can read/write **only** `users/{their_uid}`  
- No public progress listing in v1  
- Validate types lightly in rules  

### 17.5 Ads / attribution data (third parties)
AdMob / attribution may collect device advertising id, IP, device info per their policies.  
Disclose in Privacy Policy + Play Data Safety form.  
This is not stored in our custom user doc unless analytics enabled.

### 17.6 Analytics data (if enabled)
Store/send events, not full personal profiles. See §20.

---

## 18. Sync / Merge Rules

Critical: avoid progress loss when Guest later signs in.

### 18.1 When to sync
| Event | Action |
|-------|--------|
| Google sign-in success | Pull cloud → merge → push |
| Level win while signed in | Update local + push cloud (debounce OK) |
| App start signed in + network | Pull soft merge |
| Sign out | Stop cloud writes; keep local |
| Offline wins while signed in | Queue local; push when online |

### 18.2 Merge policy (locked)
For each progress field, keep the **more advanced** progress:

| Field | Merge |
|-------|-------|
| `highest_unlocked_level` | `max(local, cloud)` |
| `dog_unlocked` | `local OR cloud` |
| `level.cleared` | `true` if either true |
| settings | Prefer **local device** settings (do not overwrite mute preference from cloud unless you explicitly sync settings) |

After merge: write result to local AND cloud.

### 18.3 Conflict example
- Phone A guest cleared to Level 12  
- Same Google account cloud has Level 8  
- Sign in on Phone A → result Level 12 + push cloud  

### 18.4 New phone
- Install → Guest empty  
- Sign in Google → download cloud → restore  

---

## 19. Privacy, Consent & Legal

### 19.1 Required documents
| Doc | Need |
|-----|------|
| Privacy Policy URL | Yes (Play) |
| Terms of Service | Recommended |
| Data Safety form | Yes |
| Ads disclosure | Yes |

### 19.2 Privacy Policy must mention
- Guest local progress storage  
- Optional Google Sign-In data (id, email, name, photo)  
- Cloud save storage location (Firebase/Google Cloud)  
- AdMob / advertising identifiers  
- Analytics if used  
- Account deletion method  
- Contact email  
- Children’s policy stance (family-friendly; follow Play Families rules if targeting kids)

### 19.3 Consent UX
- First launch: short privacy notice + link + Continue  
- If using non-essential analytics in regulated regions, respect consent flags  
- Ads SDK consent / UMP (User Messaging Platform) as required by AdMob/region  

### 19.4 Settings links
- Privacy Policy  
- Delete account  
- Sign in / Sign out  

---

## 20. Analytics & Events

Optional but recommended after core fun works.  
Tool: Firebase Analytics (fits Google/Firebase stack).

### 20.1 Events to log (no stars)
| Event | Params |
|-------|--------|
| `app_open` | | 
| `tutorial_complete` | level=1 |
| `level_start` | level_id |
| `level_complete` | level_id, duration_sec |
| `level_restart` | level_id |
| `oops` | level_id |
| `hint_requested` | level_id |
| `hint_completed` | level_id |
| `dog_unlocked` | |
| `sign_in_success` | method=google |
| `sign_in_cancel` | |
| `sign_out` | |
| `cloud_sync_success` | |
| `cloud_sync_fail` | reason |
| `ad_interstitial_shown` | |
| `ad_reward_completed` | type=hint |

Do not log passwords or full unnecessary PII in event params.

---

## 21. Permissions

| Permission | Why |
|------------|-----|
| Internet | Ads, Google login, cloud sync |
| Network state | Optional soft offline checks |
| Ad ID / ATT equivalent | Via ads SDK / policy flows |
| Vibration | Optional haptics |

Do not request contacts, SMS, storage indiscriminately.

---

# D. UX / SCREENS

## 22. Full User Journeys

### 22.1 New guest
Install → Splash → Privacy continue → Home → Play → Level Select → Level 1 tutorial → win → Next…  
Soft Google prompt after Level 5 (dismissible).

### 22.2 Guest then Google
Play offline progress → Sign in → merge → cloud protected.

### 22.3 Returning Google user new phone
Install → Sign in → restore → Level Select shows cleared checkmarks.

### 22.4 Stuck player
Oops unlimited → Hint ad → win → Next.

### 22.5 Sign out
Settings → Sign out → Guest label → local progress remains.

### 22.6 Delete account
Settings → Delete → confirm → cloud wiped → signed out.

---

## 23. Screen Map & Navigation

```
Splash → Home
Home → Level Select | Settings | Sign-in sheet (modal)
Level Select → Gameplay
Gameplay → Win popup | Pause
Win → Next Gameplay | Replay | Home
Settings → Account actions | Privacy | Toggles | Back Home
```

Android Back:
- Gameplay → Pause  
- Popups → close  
- Level Select → Home  
- Home → system exit confirm optional  

---

## 24. Screen Specs (Including Account)

### Splash
Logo, short load, init Save + Auth listener + Ads init (non-blocking).

### Home
- Play  
- Settings  
- Optional Google button / profile chip if signed in  
- Optional “Save progress” banner if guest  

### Level Select
- Cat/Dog sections  
- Checkmarks for cleared  
- Locks for locked  
- Dog lock text: Clear Level 10  
- No stars  

### Gameplay
Level #, Hint, Restart, Pause. No attempts. No stars.

### Win
Freed! Next / Replay / Home. No stars.

### Settings
Music, Sound, Haptics  
Account: Sign in with Google / Sign out / Delete account  
Privacy Policy / Terms  

### Sign-in modal
Short benefit text + Google button + Not now.

### Pause
Resume / Restart / Home.

---

# E. CONTENT

## 25. Level Design Spec

20–30 levels.

| Range | Content |
|-------|---------|
| 1–8 | Teach + joy |
| 9–15 | Crossings; Level 10 unlocks Dog |
| 16–22 | Order cuts |
| 23–30 | Chains + harder |

Design first 10 on paper.

---

# F. TECHNICAL

## 26. Tech Stack

| Area | Choice |
|------|--------|
| Engine | Unity LTS 3D URP |
| Language | C# |
| Tweens | DOTween |
| Ads | Google AdMob |
| Auth | Firebase Auth + Google Sign-In |
| Cloud DB | Cloud Firestore |
| Analytics | Firebase Analytics (optional phase) |
| Crash | Firebase Crashlytics (optional phase) |
| Local save | JSON persistentDataPath (preferred) + migration from PlayerPrefs if needed |
| Git | GitHub Desktop |
| IDE | VS / VS Code |

---

## 27. Backend / Services

### Firebase project setup checklist
- [ ] Create Firebase project  
- [ ] Add Android app (package name must match Unity)  
- [ ] Download `google-services.json`  
- [ ] Enable Authentication → Google provider  
- [ ] Create Firestore database  
- [ ] Write security rules (uid-only access)  
- [ ] Enable Analytics optional  
- [ ] SHA-1/SHA-256 in Firebase for Google Sign-In debug+release  

### Cost note
Firebase free spark tier usually enough early; monitor reads/writes (debounce sync).

---

## 28. Project Folder Structure

```text
Assets/
  Art/...
  Audio/...
  Prefabs/...
  ScriptableObjects/Levels/
  Scripts/
    Core/
    Cut/
    Rope/
    Animal/
    Level/
    UI/
    Ads/
    Save/
    Auth/          # NEW
    Cloud/         # NEW
    Analytics/     # NEW optional
    Audio/
  Scenes/
  Plugins/
Docs/
  ASSET_LICENSES.txt
  PRIVACY_POLICY.md (draft)
game-plan/
  FINAL-GAME-PLAN.md   # this file
```

---

## 29. Code Architecture

```text
Bootstrap
 → SaveManager.LoadLocal
 → AuthManager.Init (listen auth state)
 → AdsManager.Init
 → AnalyticsManager.Init optional
 → Home

Sign-in success
 → CloudSaveService.Pull
 → ProgressMerger.Merge
 → SaveManager.WriteLocal
 → CloudSaveService.Push

Level win
 → SaveManager mark cleared
 → if signed in → CloudSaveService.PushDebounced
 → WinUI
 → AdsManager maybe interstitial
```

Managers: Save, Auth, Cloud, Ads, Audio, GameSession.

---

## 30. Scripts Responsibility

### Existing gameplay scripts
(Same as before: SwipeCutInput, Rope*, AnimalController, Level*, UI*, AdsManager, AudioManager…)

### Auth / data scripts (ADD)
| Script | Responsibility |
|--------|----------------|
| `AuthManager.cs` | Google sign-in/out, auth state |
| `CloudSaveService.cs` | Pull/push Firestore |
| `ProgressMerger.cs` | Merge rules |
| `LocalSaveRepository.cs` | Read/write JSON |
| `AccountUI.cs` | Sign in/out/delete UI |
| `PrivacyGateUI.cs` | First-run notice |
| `AnalyticsManager.cs` | Event logging optional |
| `NetworkStatus.cs` | Soft offline checks |

`SaveManager.cs` updated: no star fields; owns LocalProfile.

---

## 31. Level Data Format

ScriptableObject LevelData + BindingData (rope/chain, points, mustCut, orderIndex).  
No star fields.

---

## 32. Scenes & Prefabs

Scenes: Splash, Home, Game.  
Prefabs: animals, ropes, scissors FX, UI buttons, account modal.

---

## 33. Animation / Audio / Art

- Animal Idle/Happy  
- Scissors 0.2s  
- Rope sway/split  
- Free SFX packs commercial-safe  
- $0 stylized 3D assets; license log  

---

## 34. Camera, Input, UI, Performance Rules

Fixed camera; soft light; one finger swipe; big UI; safe area; no star/attempt widgets; limit particles; test real device.

---

# G. DELIVERY

## 35. Build Order

### Phase 1 — Fun core (no account yet)
1. Unity 3D URP project  
2. Cat + light + camera  
3. Perfect cut + scissors  
4. Win loop  
5. 10 levels + tutorial  
6. Local save cleared flags  
7. Home / Level Select  

### Phase 2 — Content
8. 20–30 levels  
9. Chains  
10. Dog unlock at Level 10  

### Phase 3 — Ads
11. AdMob hint + interstitial rules  

### Phase 4 — Account & cloud
12. Firebase + Google Sign-In  
13. Cloud profile + merge  
14. Account UI + delete  
15. Privacy policy + consent gate  

### Phase 5 — Release
16. Crash/analytics optional  
17. Store listing + internal test  
18. Production  

**Do not block Level 1 fun on Firebase.** Guest-first.

---

## 36. Testing & Acceptance

### Gameplay
- [ ] Cut/Oops/Win/Next  
- [ ] No stars UI  
- [ ] No attempts UI  
- [ ] Dog unlock at 10  

### Guest save
- [ ] Kill app → progress remains  
- [ ] Uninstall warning known (local loss)

### Google account
- [ ] Sign in success  
- [ ] Cancel sign-in safe  
- [ ] Offline sign-in soft fail  
- [ ] Merge keeps higher progress  
- [ ] New device restore  
- [ ] Sign out  
- [ ] Delete account removes cloud  

### Ads
- [ ] Hint rewarded  
- [ ] Interstitial only after wins  
- [ ] Never on Oops/Restart  

---

## 37. Play Store Requirements

- Package name, versioning, signing  
- Icon, feature graphic, screenshots  
- Privacy Policy URL  
- Data Safety: account info, progress cloud, ads data  
- Account deletion pathway declared  
- Content rating  
- Target API requirements  
- 64-bit  
- Internal testing → Production  

---

## 38. Marketing Ads Spec

15s real gameplay snip montage → free animal → logo.  
Muted-friendly. Real Unity capture preferred.

---

## 39. Risks & Edge Cases

| Risk | Mitigation |
|------|------------|
| Forced login hurts downloads | Guest-first |
| Merge wipes progress | max/OR merge policy + tests |
| Firebase misconfig SHA | Add debug+release SHA |
| Too many Firestore writes | Debounce pushes |
| Ad spam churn | Fair cadence |
| Cut feel weak | Fix before account work |
| Privacy rejection | Policy + delete account + Data Safety accuracy |

---

## 40. Later Roadmap

- Spinner  
- Remove Ads IAP  
- iOS + Apple Sign-In if required  
- More animals  
- Better rope physics  
- Play Games Services achievements optional  
- **Do not re-add stars/attempts unless plan explicitly changes**

---

## 41. Glossary

| Term | Meaning |
|------|---------|
| Guest | Plays without Google login |
| Auth uid | Google/Firebase user id |
| Cloud save | Firestore progress doc |
| Merge | Combine local+cloud safely |
| Cleared | Level won ≥ once |
| Oops | Wrong cut reset, unlimited |
| Rewarded | Optional ad for Hint |

---

## 42. One-Page Final Summary

**Game:** 3D animal rope-cut casual puzzle  
**Fun:** Snip + free (no stars, no attempts)  
**Progress:** Clear levels; Dog after Level 10  
**Account:** Guest default + optional Google Sign-In  
**Data:** Local JSON/profile always; cloud when signed in  
**Merge:** Keep best progress  
**Privacy:** Policy, consent, delete account  
**Ads:** Hint + every 3–4 wins  
**Build:** Fun core → content → ads → Firebase auth/cloud → store  

---

## Requirements Trace (quick)

| Need you asked | Covered in |
|----------------|------------|
| No stars | §3, §11, FR/UI |
| No attempts | §3, §12 |
| Google login | §15–16, FR-ACCOUNT |
| User data local/cloud | §17–18 |
| Nothing missing as requirements | §5–6 + all sections |
| How game works | §7–14 |
| Screens flow | §22–24 |
| Code structure | §28–30 |
| How to build | §35 |

---

## Final Line

Ship **fun first** as Guest.  
Add **Google login** to protect progress, not to block play.  
Keep this file updated when any requirement changes.

---

*End of Complete Project Requirements & Game Bible v3.0*
