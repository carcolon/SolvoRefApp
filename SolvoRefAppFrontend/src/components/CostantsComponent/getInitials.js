export const getInitials = (name) => {
  if (!name || typeof name !== "string" || name.trim() === "") {
    return "US";
  }

  const parts = name.trim().split(/\s+/).filter(Boolean);

  let initials = "";
  if (parts.length > 0) {
    initials += parts[0][0].toUpperCase();
  }

  if (parts.length >= 4) {
    if (parts[2] && parts[2].length > 0) {
      initials += parts[2][0].toUpperCase();
    }
  } else if (parts.length === 2 || parts.length === 3) {
    if (parts[parts.length - 1] && parts[parts.length - 1].length > 0) {
      initials += parts[parts.length - 1][0].toUpperCase();
    }
  } else if (
    parts.length === 1 &&
    initials.length === 1 &&
    parts[0].length > 1
  ) {
    initials += parts[0][1].toUpperCase();
  }

  if (initials.length === 0) {
    return "US";
  } else if (initials.length === 1) {
    if (name.trim().length >= 2) {
      return name.trim().substring(0, 2).toUpperCase();
    }
    return initials + initials;
  }

  return initials.substring(0, 2);
};
