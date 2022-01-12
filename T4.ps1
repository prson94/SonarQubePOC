"*************************************************"
" T4 Template converter                           "
"*************************************************"

Get-ChildItem -Path .\ -Filter *.tt -Recurse -File -Name| ForEach-Object {
    $file = [System.IO.Path]::GetFileName($_);
    $directory = [System.IO.Path]::GetDirectoryName($_)
    "Conversion file : " + $file
    t4 "$directory\$file" -I="$directory"
}

"All t4 templates converted!"
