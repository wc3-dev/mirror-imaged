-- Sounds (W5-2): which file GetSoundFileDuration reads (the install keeps a classic file, and for most sounds an HD and
-- a DE one of another length), what GetSoundDuration gives for each way of making a sound, and when a sound reads
-- playing. Every volume group is muted first, so the probe makes no sound.
for i = 0, 14 do VolumeGroupSetVolume(ConvertVolumeGroup(i), 0) end
StopMusic(false)

local function file(path) out('file ' .. path .. ' ' .. GetSoundFileDuration(path)) end
file('Units\\Human\\HeroPaladin\\HeroPaladinWhat1.wav')    -- classic ogg 1288, HD and DE 1788
file('Units\\Human\\HeroPaladin\\HeroPaladinWhat1.flac')
file('Units\\Human\\HeroPaladin\\HeroPaladinWhat1.ogg')
file('Units\\Human\\HeroPaladin\\HeroPaladinWhat1.mp3')
file('Units\\Human\\HeroPaladin\\HeroPaladinWhat1')
file('units/human/heropaladin/heropaladinwhat1.wav')
file('Units\\Human\\HeroPaladin\\HeroPaladinWhat2.flac')   -- classic 1184, DE 1684
file('Sound\\Dialogue\\HumanCampaign\\Human01\\H01Arthas02.flac') -- classic mp3 4466 by its frames, DE 4830
file('Sound\\Music\\mp3Music\\Human1.mp3')                 -- classic mp3 273031, DE flac 272956
file('Sound\\Music\\mp3Music\\PH1.mp3')                    -- only an mp3: 281573
file('Sound\\Music\\mp3Music\\PH1.flac')
file('Sound\\Music\\mp3Music\\Combat1.ogg')                -- only a classic ogg: 107050
file('Sound\\Interface\\MouseClick1.flac')                 -- classic 239, DE 238
file('Sound\\Interface\\QuestNew.wav')                     -- only a classic ogg: 3750
file('Abilities\\Spells\\Human\\HeroicSlash\\HeroicSlash0.ogg') -- only HD and DE
file('Sound\\NoSuchFile.wav')
file('')
file('QuestNew')
file('Comradeship')

local paladin = 'Units\\Human\\HeroPaladin\\HeroPaladinWhat1.wav'
local function make() return CreateSound(paladin, false, false, false, 10, 10, 'DefaultEAXON') end
local function state(s)
    return string.format('duration %d playing %s loading %s', GetSoundDuration(s), tostring(GetSoundIsPlaying(s)), tostring(GetSoundIsLoading(s)))
end
local function show(name, s) out(name .. ' ' .. (s == nil and 'nil' or state(s))) end

local before = CreateTimer()
local plain = make()
local after = CreateTimer()
out('ids ' .. (GetHandleId(plain) - GetHandleId(before)) .. ' ' .. (GetHandleId(after) - GetHandleId(plain)))
show('plain', plain)
local set = make()
SetSoundDuration(set, 3000)
show('set 3000', set)
local negative = make()
SetSoundDuration(negative, -5)
show('set -5', negative)
local params = make()
SetSoundParamsFromLabel(params, 'QuestNew')
show('params QuestNew', params)
show('missing', CreateSound('Sound\\NoSuchFile.wav', false, false, false, 10, 10, 'DefaultEAXON'))
local label = CreateSoundFromLabel('QuestNew', false, false, false, 10000, 10000)
show('label QuestNew', label)
show('label HeroPaladinWhat', CreateSoundFromLabel('HeroPaladinWhat', false, false, false, 10000, 10000))
show('label none', CreateSoundFromLabel('NoSuchLabel', false, false, false, 10000, 10000))
show('midi RoosterSound', CreateMIDISound('RoosterSound', 20, 20))
show('midi LordaeronSummerDay', CreateMIDISound('LordaeronSummerDay', 20, 20))
show('midi none', CreateMIDISound('NoSuchLabel', 20, 20))
show('file with label QuestNew', CreateSoundFilenameWithLabel(paladin, false, false, false, 10, 10, 'QuestNew'))
out('nil ' .. GetSoundDuration(nil) .. ' ' .. tostring(GetSoundIsPlaying(nil)) .. ' ' .. tostring(GetSoundIsLoading(nil)))

local function q(text) return text == nil and 'nil' or '"' .. text .. '"' end
out(string.format('speaker %s %s %s', q(GetDialogueSpeakerNameKey(plain)), tostring(SetDialogueSpeakerNameKey(plain, 'TRIGSTR_70')), q(GetDialogueSpeakerNameKey(plain))))
out(string.format('text %s %s %s', q(GetDialogueTextKey(plain)), tostring(SetDialogueTextKey(plain, 'Some text')), q(GetDialogueTextKey(plain))))
out(string.format('dialogue nil %s %s %s', tostring(GetDialogueSpeakerNameKey(nil)), tostring(SetDialogueSpeakerNameKey(nil, 'x')), tostring(SetDialogueTextKey(nil, 'x'))))
out(string.format('facial %s %s %s', tostring(SetSoundFacialAnimationLabel(plain, 'x')), tostring(SetSoundFacialAnimationGroupLabel(plain, 'Map-Arthas')),
    tostring(SetSoundFacialAnimationSetFilepath(plain, 'Sound/Dialogue/FaceAnimation/Human01/FacialAnimation/Arthas.animset'))))
out(string.format('facial nil %s %s %s', tostring(SetSoundFacialAnimationLabel(nil, 'x')), tostring(SetSoundFacialAnimationGroupLabel(nil, 'x')),
    tostring(SetSoundFacialAnimationSetFilepath(nil, 'x'))))

-- Started at 0: read now, at 0.6, 2.0 and 4.5 s; QuestNew is 3750 ms, the Paladin 1288 ms (or 1788)
local loop = CreateSound(paladin, true, false, false, 10, 10, 'DefaultEAXON')
local missing = CreateSound('Sound\\NoSuchFile.wav', false, false, false, 10, 10, 'DefaultEAXON')
local twice, stopped, faded, killed, idle, stopKilled, position, positionFirst, again, fast =
    make(), make(), make(), make(), make(), make(), make(), make(), make(), make()
local started = {
    {'plain', plain}, {'set 3000', set}, {'set -5', negative}, {'loop', loop}, {'missing', missing}, {'label', label},
    {'twice', twice}, {'stopped', stopped}, {'faded', faded}, {'killed', killed}, {'idle killed', idle},
    {'stopped killed', stopKilled}, {'position', position}, {'position first', positionFirst}, {'again', again}, {'pitch 2', fast},
}
SetSoundPlayPosition(positionFirst, 1000)
SetSoundPitch(fast, 2.0)
for _, entry in ipairs(started) do
    if entry[2] ~= idle then StartSound(entry[2]) end
end
StartSound(twice)
StopSound(stopped, false, false)
StopSound(faded, false, true)
KillSoundWhenDone(killed)
KillSoundWhenDone(idle)
StopSound(stopKilled, true, false)
SetSoundPlayPosition(position, 1000)
local function read(time)
    for _, entry in ipairs(started) do out(time .. ' ' .. entry[1] .. ' ' .. state(entry[2])) end
end
read('0.0')
TimerStart(CreateTimer(), 0.6, false, guard(function()
    read('0.6')
    StartSound(again)
    out('0.6 again started ' .. state(again))
end))
TimerStart(CreateTimer(), 0.8, false, guard(function() out('0.8 again ' .. state(again)) end))
TimerStart(CreateTimer(), 2.0, false, guard(function()
    read('2.0')
    StartSound(plain)
    out('2.0 plain started again ' .. state(plain))
end))
TimerStart(CreateTimer(), 2.5, false, guard(function() out('2.5 plain ' .. state(plain)) end))
TimerStart(CreateTimer(), 4.5, false, guard(function() read('4.5') end))

-- TriggerWaitForSound in an action, from 5 s: how long it waits, read on a clock timer
TimerStart(CreateTimer(), 5.0, false, guard(function()
    local trigger = CreateTrigger()
    TriggerAddAction(trigger, guard(function()
        local clock = CreateTimer()
        TimerStart(clock, 1000.0, false, nil)
        local function waited(name) out(string.format('wait %s %.1f', name, TimerGetElapsed(clock))) end
        local s = make()
        StartSound(s)
        TriggerWaitForSound(s, 0)
        waited('0')
        s = make()
        StartSound(s)
        TriggerWaitForSound(s, 1.0)
        waited('1.0')
        TriggerWaitForSound(make(), 0)
        waited('idle')
        done()
    end))
    TriggerExecute(trigger)
end))
