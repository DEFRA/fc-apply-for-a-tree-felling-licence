/* 
    These users should be in AZ B2C, but NOT have any user account data in FLOv2 db prior to test run
*/
const AuthenticatedButUnregisteredFloUsers= [
    { 
        email: "Flov2_Perf_1@qxlva.com",
        password: "Flov2_Perf_1!" 
    },
    { 
        email: "Flov2_Perf_3@qxlva.com",
        password: "Flov2_Perf_3!" 
    },
    { 
        email: "Flov2_Perf_4@qxlva.com",
        password: "Flov2_Perf_4!" 
    }
];

/* 
    These users should have both AZ B2C and associated user accounts in FLOv2 db
*/
const RegisteredFloUsers = [
    { 
        email: "Flov2_Perf_2@qxlva.com",
        password: "Flov2_Perf_2!" 
    }
    //etc, flo perf 5 and 6 exist, but not in seed data.
];

const UnknownUser = {
    email: "unknown.user@qxlva.com",
    password: "password"
}

function GetUserByEmail (username)  {
   
    var match = this.AuthenticatedButUnregisteredFloUsers.find(u=>u.email.toLowerCase() === username.toLowerCase());

    if (match) {
        return match;
    }

    match = this.RegisteredFloUsers.find(u=>u.email.toLowerCase() === username.toLowerCase());

    if (match) 
    {
        return match;
    }

    if (username.toLowerCase()=== UnknownUser.email.toLowerCase()){
        return UnknownUser;        
    }
};

module.exports = {
    AuthenticatedButUnregisteredFloUsers, 
    RegisteredFloUsers,
    GetUserByEmail,
    UnknownUser 
};
