-- W1-5's files-text: Preloader of a file whose line isn't JASS closes the game (exit code 1), before the line.
PreloadGenClear()
PreloadGenStart()
Preload("\")\nnot jass\n//")
PreloadGenEnd("MirrorImaged\\files-probe\\text.pld")
Preloader("MirrorImaged\\files-probe\\text.pld")
out("read")
done()
