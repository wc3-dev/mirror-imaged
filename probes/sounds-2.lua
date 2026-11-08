-- Sounds, the second probe (W5-2): the lengths of the install's classic mp3 files, which a frame count reads a frame or
-- more off; when a sound stops reading playing, polled every 0.05 s; how long TriggerWaitForSound waits; and whether a
-- label of several files gives one length. Every volume group is muted first, so the probe makes no sound.
for i = 0, 14 do VolumeGroupSetVolume(ConvertVolumeGroup(i), 0) end
StopMusic(false)

for _, name in ipairs({'ArthasTheme', 'BloodElfTheme', 'Comradeship', 'Credits', 'DarkAgents', 'DarkVictory', 'Doom',
    'HeroicVictory', 'Human1', 'Human2', 'Human3', 'HumanDefeat', 'HumanVictory', 'HumanX1', 'IllidansTheme',
    'LichKingTheme', 'Mainscreen', 'NagaTheme', 'NightElf1', 'NightElf2', 'NightElf3', 'NightElfDefeat',
    'NightElfVictory', 'NightElfX1', 'Orc1', 'Orc2', 'Orc3', 'OrcDefeat', 'OrcTheme', 'OrcVictory', 'OrcX1', 'PH1',
    'PursuitTheme', 'SadMystery', 'Tension', 'TragicConfrontation', 'Undead1', 'Undead2', 'Undead3', 'UndeadDefeat',
    'UndeadVictory', 'UndeadX1', 'War2IntroMusic', 'War3XMainScreen'}) do
    out('music ' .. name .. ' ' .. GetSoundFileDuration('Sound\\Music\\mp3Music\\' .. name .. '.mp3'))
end
for _, path in ipairs({'HumanCampaign\\Human01\\H01VillagerF27', 'HumanCampaign\\Human01\\H01VillagerM28',
    'HumanCampaign\\Human02\\H02Blademaster11', 'HumanCampaign\\Human01\\H01Arthas04', 'HumanCampaign\\Human01\\H01Arthas06',
    'HumanCampaign\\Human01\\H01Arthas08', 'HumanCampaign\\Human01\\H01Arthas24', 'HumanCampaign\\Human01\\H01Grunt21',
    'HumanExpCamp\\Human07x\\A07Illidan14', 'HumanCampaign\\Human08\\H08Captain01', 'HumanExpCamp\\Human01x\\A01Garithos05',
    'NightElfExpCamp\\NightElf02x\\S02Goblin40', 'HumanCampaign\\Human05\\H05VillagerM47', 'HumanExpCamp\\Human01x\\A01Kael11',
    'HumanCampaign\\Human06Interlude\\H06Uther51', 'HumanExpCamp\\Human02x\\A02Kael06',
    'HumanExpCamp\\Human02xInterlude\\A02Garithos38'}) do
    out('dialogue ' .. path .. ' ' .. GetSoundFileDuration('Sound\\Dialogue\\' .. path .. '.flac'))
end

local lengths = {}
for i = 1, 6 do
    lengths[i] = GetSoundDuration(CreateSoundFromLabel('HeroPaladinWhat', false, false, false, 10000, 10000))
end
out('label HeroPaladinWhat ' .. table.concat(lengths, ' '))
local pissed = CreateSoundFromLabel('HeroPaladinPissed', false, false, false, 10000, 10000)
out('label HeroPaladinPissed ' .. GetSoundDuration(pissed))

-- When each stops reading playing: QuestNew is 3750 ms, the Paladin 1288 ms
local paladin = 'Units\\Human\\HeroPaladin\\HeroPaladinWhat1.wav'
local function make() return CreateSound(paladin, false, false, false, 10, 10, 'DefaultEAXON') end
local sounds = {}
local function add(name, s) sounds[#sounds + 1] = {name, s} return s end
local plain = add('plain', make())
local function set(name, length) local s = add(name, make()) SetSoundDuration(s, length) return s end
set('set 3000', 3000)
set('set 500', 500)
set('set 0', 0)
set('set -5', -5)
local twice = add('twice', make())
local again = add('again 0.6', make())
local position = add('position 1000', make())
local positionFirst = add('position first 1000', make())
SetSoundPlayPosition(positionFirst, 1000)
SetSoundPitch(add('pitch 2', make()), 2.0)
SetSoundPitch(add('pitch 0.5', make()), 0.5)
add('label QuestNew', CreateSoundFromLabel('QuestNew', false, false, false, 10000, 10000))
add('loop', CreateSound(paladin, true, false, false, 10, 10, 'DefaultEAXON'))
local faded = add('faded 0.5', make())
local killed = add('killed', make())
add('3D', CreateSound(paladin, false, true, true, 10, 10, 'DefaultEAXON'))
for _, entry in ipairs(sounds) do StartSound(entry[2]) end
StartSound(twice)
SetSoundPlayPosition(position, 1000)
KillSoundWhenDone(killed)

local playing, time = {}, 0
for i, entry in ipairs(sounds) do playing[i] = GetSoundIsPlaying(entry[2]) end
local poll = CreateTimer()
TimerStart(poll, 0.05, true, guard(function()
    time = time + 0.05
    if time > 0.59 and time < 0.61 then StartSound(again) end
    if time > 0.49 and time < 0.51 then StopSound(faded, false, true) end
    for i, entry in ipairs(sounds) do
        local now = GetSoundIsPlaying(entry[2])
        if now ~= playing[i] then out(string.format('%s %s at %.2f', entry[1], now and 'plays' or 'stops', time)) end
        playing[i] = now
    end
    if time > 5.99 then
        PauseTimer(poll)
        for i, entry in ipairs(sounds) do
            if playing[i] then out(entry[1] .. ' plays at 6') end
        end
    end
end))

-- TriggerWaitForSound in an action, from 6.5 s: how long it waits, read on a clock timer
TimerStart(CreateTimer(), 6.5, false, guard(function()
    local trigger = CreateTrigger()
    TriggerAddAction(trigger, guard(function()
        local clock = CreateTimer()
        TimerStart(clock, 1000.0, false, nil)
        local last = 0
        local function waited(name)
            out(string.format('wait %s %.1f', name, TimerGetElapsed(clock) - last))
            last = TimerGetElapsed(clock)
        end
        local s = make()
        StartSound(s)
        TriggerWaitForSound(s, 0)
        waited('0')
        TriggerWaitForSound(s, 0)
        waited('ended')
        s = make()
        StartSound(s)
        TriggerWaitForSound(s, 0.5)
        waited('0.5')
        s = set('wait set 3000', 3000)
        StartSound(s)
        TriggerWaitForSound(s, 0)
        waited('set 3000')
        TriggerWaitForSound(make(), 0)
        waited('idle')
        TriggerWaitForSound(make(), 2)
        waited('idle 2')
        TriggerWaitForSound(nil, 0)
        waited('nil')
        s = CreateSoundFromLabel('QuestNew', false, false, false, 10000, 10000)
        StartSound(s)
        TriggerWaitForSound(s, -1)
        waited('label -1')
        done()
    end))
    TriggerExecute(trigger)
end))
