-- ParseTags on the data probe's map (DataProbeMaps.Data, where Holy Light heals 250 at level 1), apart from the
-- data probe in case a text closes the game.
local function show(value)
    return value == nil and "nil" or tostring(value)
end

for _, text in ipairs({ "plain", "<AHhb,DataA1>", "heal <AHhb,DataA1> or <AHhb,DataA2>", "<AHhb,Cool1>",
    "<AHhb,DataA1,%>", "<Rhme,base1>", "<hfoo,realHP>", "<hfoo,HP>", "<zzzz,DataA1>", "<AHhb>", "TRIGSTR_001",
    "%d <AHhb,DataA1>", "<AHhb,DataA2>", "<Aslo,DataA1>", "<Aslo,DataA1,%>", "<Aslo,DataB1,%>", "<Aslo,Area1>",
    "<ahhb,dataa2>", "<AHhb,DataA4>", "<AHhb,Name>", "<AHhb,levels>", "<AHtb,Dur1>", "<AHhb,DataA2", "<<AHhb,DataA2>>",
    "<AHhb,DataA2,x>", "<hfoo,spd>", "<hfoo,dmgplus1>", "<hfoo,cool1>", "<Hpal,STR>" }) do
    out("tags [" .. text .. "] [" .. show(ParseTags(text)) .. "]")
end

done()
