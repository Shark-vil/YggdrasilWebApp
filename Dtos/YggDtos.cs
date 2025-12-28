namespace YggdrasilWebApp.Dtos;

// ---------- Authserver requests ----------
public sealed record AuthenticateRequest(
	string Username,
	string Password,
	string? ClientToken,
	bool? RequestUser
);

public sealed record RefreshRequest(
	string AccessToken,
	string? ClientToken,
	YggProfileRef? SelectedProfile,
	bool? RequestUser
);

public sealed record ValidateRequest(
	string AccessToken,
	string? ClientToken
);

public sealed record InvalidateRequest(
	string AccessToken,
	string? ClientToken
);

public sealed record SignoutRequest(
	string Username,
	string Password
);

// ---------- Sessionserver requests ----------
public sealed record JoinRequest(
	string AccessToken,
	string SelectedProfile,
	string ServerId
);

// ---------- Common / responses ----------
public sealed record YggProfileRef(string Id, string Name);

public sealed record YggUser(string Id, List<YggUserProperty> Properties);
public sealed record YggUserProperty(string Name, string Value);

public sealed record AuthenticateResponse(
	string AccessToken,
	string ClientToken,
	List<YggProfileRef> AvailableProfiles,
	YggProfileRef? SelectedProfile,
	YggUser? User
);

public sealed record RefreshResponse(
	string AccessToken,
	string ClientToken,
	YggProfileRef? SelectedProfile,
	YggUser? User
);

// ---------- Profile responses ----------
public sealed record ProfileProperty(string Name, string Value, string? Signature);

public sealed record SessionProfileResponse(
	string Id,
	string Name,
	List<ProfileProperty> Properties
);

public sealed record SimpleProfileResponse(
	string Id,
	string Name
);