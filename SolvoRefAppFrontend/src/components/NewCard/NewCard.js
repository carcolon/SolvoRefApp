import React, {
  useState,
  useMemo,
  useRef,
  useEffect,
  useCallback,
} from "react";
import { Tooltip } from "react-tooltip";

const NewCard = ({
  user,
  provided,
  recruiterUsers,
  handleUserUpdate,
  rInitial,
  rColor,
  formatDate,
  showExtraInformation = false,
}) => {
  const [isSelectorOpen, setIsSelectorOpen] = useState(false);
  const [recruiterFilter, setRecruiterFilter] = useState("");
  const selectorRef = useRef(null);

  const assignedRecruiterUser = useMemo(() => {
    if (user.assignedRecruiterId) {
      return recruiterUsers.find((rec) => rec.id === user.assignedRecruiterId);
    }
    return null;
  }, [user.assignedRecruiterId, recruiterUsers]);

  const showTimeBadge =
    user.status === "In Progress" || user.status === "First Contact";

  const handleRecruiterInitialsClick = useCallback((e) => {
    e.stopPropagation();
    setIsSelectorOpen((prev) => !prev);
    setRecruiterFilter("");
  }, []);

  const handleUserSelect = useCallback(
    (selectedUser) => {
      handleUserUpdate(user.id, selectedUser);
      setIsSelectorOpen(false);
      setRecruiterFilter("");
    },
    [handleUserUpdate, user.id]
  );

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (selectorRef.current && !selectorRef.current.contains(event.target)) {
        setIsSelectorOpen(false);
        setRecruiterFilter("");
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  const filteredRecruiterUsers = useMemo(() => {
    if (!recruiterFilter) {
      return recruiterUsers;
    }
    const lowercasedFilter = recruiterFilter.toLowerCase();
    return recruiterUsers.filter((u) =>
      u.name.toLowerCase().includes(lowercasedFilter)
    );
  }, [recruiterUsers, recruiterFilter]);

  return (
    <div
      className={`movable-item ${user.status
        .toLowerCase()
        .replace(/\s/g, "-")}-card`}
      ref={provided.innerRef}
      {...provided.draggableProps}
      {...provided.dragHandleProps}
    >
      <div className="custom-card-content">
        <div className="card-heade">
          <h3 className="card-name">{user.name}</h3>
          <span className="emails">{user?.email}</span>
          <br />
          <span className="telefonos">{user?.phoneNumber}</span>
        </div>

        {user.referredBy && (
          <div className="card-referred-by">
            <span className="label">Referred by:</span>
            <span className="value"> {user.referredBy}</span>
          </div>
        )}

        <div className="card-foote">
          <p className="card-dat">{user.date}</p>
          {showTimeBadge ? (
            <div className="in-progress-badge">
              <span role="img" aria-label="clock"></span>
              <span className="card-met">{formatDate}</span>
            </div>
          ) : (
            <span className="card-met">{formatDate}</span>
          )}
        </div>

        {/* Selector de Reclutadores */}
        <div className="card-initials-selector-container">
          <div
            className="card-initials-display"
            style={{ backgroundColor: rColor }}
            onClick={handleRecruiterInitialsClick}
            data-tooltip-id={`recruiter-tooltip-${user.id}`}
            data-tooltip-content={
              assignedRecruiterUser
                ? assignedRecruiterUser.name
                : "Select Recruiter"
            }
            data-tooltip-place="bottom"
          >
            <h6>{rInitial}</h6>
          </div>
          <Tooltip id={`recruiter-tooltip-${user.id}`} />

          {isSelectorOpen && (
            <div
              style={{ width: "30px", right: "-20px" }}
              className="mkt-users-dropdown"
              ref={selectorRef}
            >
              <input
                style={{ width: "150px", position: "relative", right: "-10px" }}
                type="text"
                placeholder="Filter recruiters..."
                value={recruiterFilter}
                onChange={(e) => setRecruiterFilter(e.target.value)}
                onClick={(e) => e.stopPropagation()}
              />
              <ul>
                {filteredRecruiterUsers.length > 0 ? (
                  filteredRecruiterUsers.map((recruiterUser) => (
                    <li
                      key={recruiterUser.id}
                      onClick={() => handleUserSelect(recruiterUser)}
                      className={
                        assignedRecruiterUser &&
                        assignedRecruiterUser.id === recruiterUser.id
                          ? "selected"
                          : ""
                      }
                    >
                      <div
                        className="mkt-user-option-initials"
                        style={{
                          backgroundColor: rColor,
                        }}
                      >
                        {rInitial}
                      </div>
                      <span className="mkt-user-option-name">
                        {recruiterUser.name}
                      </span>
                    </li>
                  ))
                ) : (
                  <div className="no-results-message">No recruiters found</div>
                )}
              </ul>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default NewCard;
