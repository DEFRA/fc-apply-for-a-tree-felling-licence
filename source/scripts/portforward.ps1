kubectl config use-context fs_flov2
cmd.exe /c START /B kubectl port-forward service/mail 587 -n postfix
cmd.exe /c START /B kubectl port-forward service/mailhog 8025 -n mailhog