-- Which letters StringHash folds, as pairs of lower and upper case in UTF-8; and numbers given to string parameters.
local pairs_ = {
    {'l-stroke', '\197\130', '\197\129'}, {'a-ogonek', '\196\133', '\196\132'}, {'z-caron', '\197\190', '\197\189'},
    {'s-caron', '\197\161', '\197\160'}, {'o-double-acute', '\197\145', '\197\144'}, {'oe', '\197\147', '\197\146'},
    {'dotless-i', '\196\177', 'I'}, {'cyrillic-io', '\209\145', '\208\129'}, {'cyrillic-ya', '\209\143', '\208\175'},
    {'armenian-ayb', '\213\161', '\212\177'}, {'fullwidth-a', '\239\189\129', '\239\188\161'},
    {'dz-caron', '\199\134', '\199\132'}, {'alpha', '\206\177', '\206\145'}, {'micro', '\194\181', '\206\156'},
    {'y-diaeresis', '\195\191', '\197\184'}, {'o-diaeresis', '\195\182', '\195\150'}, {'thorn', '\195\190', '\195\158'},
    {'divide-times', '\195\183', '\195\151'}, {'hangul', '\234\176\128', '\234\176\128'},
}
for _, p in ipairs(pairs_) do
    out('StringHash ' .. p[1] .. ' = ' .. StringHash(p[2]) .. ' ' .. StringHash(p[3]) .. ' ' .. tostring(StringHash(p[2]) == StringHash(p[3])))
end
out('StringCase cyrillic-io true = ' .. StringCase('\209\145\208\129', true))
for _, v in ipairs({1e20, 3.0, -0.0, 0.1, 123456789, 2 ^ 31, 1 / 3}) do
    out('StringLength ' .. string.format('%.9g', v) .. ' ' .. math.type(v) .. ' = ' .. StringLength(v) .. ' ' .. SubString(v, 0, 20))
end
done()
