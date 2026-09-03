import React, {
  useState,
  useRef,
  useEffect,
  useCallback,
  useMemo,
} from "react";
import "./cards.css";
import { getInitials } from "../CostantsComponent/getInitials";
import Swal from "sweetalert2";
import { Tooltip } from "react-tooltip";
import { COLORS_ARRAY } from "../CostantsComponent/costants";
import ModalDetalleCardsMKT_RECR from "../ModalCopmponent/ModalDetalleCardsMKT_RECR";
import { formatTimeElapsed } from "../CostantsComponent/timeElapsed";

function CardMkt({
  isActive,
  columns,
  handleUpdateColumns,
  mktUsers = [],
  isLoadingMktUsers = false,
  mktUsersError = null,
  setIsDragging,
}) {
  const [openMktSelectorItemId, setOpenMktSelectorItemId] = useState(null);
  const [mktFilter, setMktFilter] = useState("");
  // Estado y manejadores del modal
  const [selectedCard, setSelectedCard] = useState(null);

  const selectorRefs = useRef({});
  const documentClickListenerRef = useRef(null);
  const [, setUpdater] = useState(0);

  // Manejador para abrir el modal
  const handleCardClick = useCallback((cardItem) => {
    setSelectedCard(cardItem);
  }, []);

  // Manejador para cerrar el modal
  const handleCloseModal = useCallback(() => {
    setSelectedCard(null);
  }, []);

  useEffect(() => {
    const intervalId = setInterval(() => {
      setUpdater((prev) => prev + 1);
    }, 1000);

    return () => clearInterval(intervalId);
  }, []);

  useEffect(() => {
    if (documentClickListenerRef.current) {
      document.removeEventListener(
        "mousedown",
        documentClickListenerRef.current
      );
    }

    const handleClickOutside = (event) => {
      if (openMktSelectorItemId) {
        const selectorElement = selectorRefs.current[openMktSelectorItemId];
        if (selectorElement && !selectorElement.contains(event.target)) {
          setOpenMktSelectorItemId(null);
          setMktFilter("");
        }
      }
    };

    documentClickListenerRef.current = handleClickOutside;
    document.addEventListener("mousedown", documentClickListenerRef.current);

    return () => {
      if (documentClickListenerRef.current) {
        document.removeEventListener(
          "mousedown",
          documentClickListenerRef.current
        );
      }
    };
  }, [openMktSelectorItemId]);

  const handleMktInitialsClick = useCallback(
    (e, itemId) => {
      e.stopPropagation(); // Evita que el clic en las iniciales abra la modal
      setOpenMktSelectorItemId(
        openMktSelectorItemId === itemId ? null : itemId
      );
      setMktFilter("");
    },
    [openMktSelectorItemId]
  );

  const handleMktUserSelect = useCallback(
    (columnId, itemId, selectedUser) => {
      handleUpdateColumns((prevColumns) => {
        const newColumns = { ...prevColumns };
        const columnIndex = newColumns[columnId].findIndex(
          (item) => item.id === itemId
        );

        if (columnIndex !== -1) {
          const updatedItem = {
            ...newColumns[columnId][columnIndex],
            content: {
              ...newColumns[columnId][columnIndex].content,
              selectedMktUser: selectedUser,
              assignedMktUserId: selectedUser.id,
            },
          };
          newColumns[columnId] = [
            ...newColumns[columnId].slice(0, columnIndex),
            updatedItem,
            ...newColumns[columnId].slice(columnIndex + 1),
          ];
        }
        return newColumns;
      });
      setOpenMktSelectorItemId(null);
      setMktFilter("");
    },
    [handleUpdateColumns]
  );

  const [draggingItemData, setDraggingItemData] = useState(null);
  const [dragOverTarget, setDragOverTarget] = useState({
    columnId: null,
    itemId: null,
    position: null,
  });

  const handleDragStart = useCallback(
    (e, itemId, sourceColumnId) => {
      setIsDragging(true);
      if (sourceColumnId === "card-2") {
        e.preventDefault();
        Swal.fire({
          title: "Movement Denied",
          text: 'This reference is marked as "Banned" and cannot be edited.',
          icon: "info",
          confirmButtonColor: "rgb(48, 192, 209)",
          confirmButtonText: "Ok",
        });
        return;
      }

      if (sourceColumnId === "card-4") {
        e.preventDefault();
        Swal.fire({
          title: "Movement Denied",
          text: 'Items in the "In Progress" column cannot be moved once set.',
          icon: "info",
          confirmButtonColor: "rgb(48, 192, 209)",
          confirmButtonText: "Ok",
        });
        return;
      }

      setDraggingItemData({ itemId, sourceColumnId });
      e.dataTransfer.setData(
        "application/json",
        JSON.stringify({ itemId, sourceColumnId })
      );
      e.dataTransfer.effectAllowed = "move";
      e.currentTarget.classList.add("is-dragging");
    },
    [setIsDragging]
  );

  const handleDragEnd = useCallback((e) => {
    setDraggingItemData(null);
    setDragOverTarget({ columnId: null, itemId: null, position: null });
    if (e.currentTarget) {
      e.currentTarget.classList.remove("is-dragging");
    }
  }, []);

  const handleColumnDragOver = useCallback(
    (e, columnId) => {
      e.preventDefault();
      e.dataTransfer.dropEffect = "move";

      if (
        draggingItemData &&
        (draggingItemData.sourceColumnId === "card-2" ||
          draggingItemData.sourceColumnId === "card-4")
      ) {
        e.dataTransfer.dropEffect = "none";
        setDragOverTarget({ columnId: null, itemId: null, position: null });
        return;
      }

      const isOverColumnOrPlaceholder =
        e.target.classList.contains("card") ||
        e.target.classList.contains("no-items");

      if (isOverColumnOrPlaceholder) {
        setDragOverTarget({
          columnId: columnId,
          itemId: null,
          position: "bottom",
        });
      } else {
        setDragOverTarget({ columnId: null, itemId: null, position: null });
      }
    },
    [draggingItemData]
  );

  const handleColumnDragLeave = useCallback((e) => {
    if (!e.currentTarget.contains(e.relatedTarget)) {
      setDragOverTarget({ columnId: null, itemId: null, position: null });
    }
  }, []);

  const handleItemDragOver = useCallback(
    (e, targetItemId, targetColumnId) => {
      e.preventDefault();
      e.stopPropagation();
      e.dataTransfer.dropEffect = "move";

      if (
        draggingItemData &&
        (draggingItemData.sourceColumnId === "card-2" ||
          draggingItemData.sourceColumnId === "card-4")
      ) {
        e.dataTransfer.dropEffect = "none";
        setDragOverTarget({ columnId: null, itemId: null, position: null });
        return;
      }

      if (draggingItemData && draggingItemData.itemId === targetItemId) {
        setDragOverTarget({ columnId: null, itemId: null, position: null });
        return;
      }

      const rect = e.currentTarget.getBoundingClientRect();
      const y = e.clientY - rect.top;
      const isNearTop = y < rect.height / 2;

      setDragOverTarget({
        columnId: targetColumnId,
        itemId: targetItemId,
        position: isNearTop ? "top" : "bottom",
      });
    },
    [draggingItemData]
  );

  const handleDrop = useCallback(
    async (e, targetColumnId, targetItemId = null, dropPosition = null) => {
      e.preventDefault();
      e.stopPropagation();

      const data = JSON.parse(e.dataTransfer.getData("application/json"));
      const { itemId: draggedItemId, sourceColumnId } = data;

      setDragOverTarget({ columnId: null, itemId: null, position: null });

      if (
        !draggedItemId ||
        sourceColumnId === "card-2" ||
        sourceColumnId === "card-4"
      ) {
        return;
      }

      if (targetColumnId === "card-2" && sourceColumnId !== "card-2") {
        const result = await Swal.fire({
          title: "Alert",
          text: "Are you sure you want to mark this reference as 'Banned'? This action is permanent and cannot be undone.",
          icon: "warning",
          showCancelButton: true,
          confirmButtonColor: "rgb(220, 112, 56)",
          cancelButtonColor: "rgb(48, 192, 209)",
          confirmButtonText: "Continue",
          cancelButtonText: "Cancel",
          reverseButtons: true,
        });
        if (!result.isConfirmed) {
          return;
        }
      }

      if (targetColumnId === "card-4" && sourceColumnId !== "card-4") {
        const result = await Swal.fire({
          title: "Alert",
          text: "Are you sure you want to move this reference to 'In Progress'? This action cannot be undone and you will no longer be able to modify the status. The reference will be forwarded to the recruiting team.",
          icon: "warning",
          showCancelButton: true,
          confirmButtonColor: "rgb(220, 112, 56)",
          cancelButtonColor: "rgb(48, 192, 209)",
          confirmButtonText: "Continue",
          cancelButtonText: "Cancel",
          reverseButtons: true,
        });
        if (!result.isConfirmed) {
          return;
        }
      }

      handleUpdateColumns((prevColumns) => {
        const newColumns = { ...prevColumns };
        const sourceItems = [...newColumns[sourceColumnId]];
        const draggedItemIndex = sourceItems.findIndex(
          (item) => item.id === draggedItemId
        );
        if (draggedItemIndex === -1) {
          return prevColumns;
        }

        const [movedItem] = sourceItems.splice(draggedItemIndex, 1);

        // Update the item's creation date when it enters the "In Progress" column
        if (targetColumnId === "card-4") {
          movedItem.content.inProgressStartDate = new Date().toISOString();
        }

        if (sourceColumnId !== targetColumnId) {
          const targetItems = [...newColumns[targetColumnId]];
          targetItems.splice(targetItems.length, 0, movedItem);
          newColumns[targetColumnId] = targetItems;
          newColumns[sourceColumnId] = sourceItems;
        } else {
          let targetItems = [...sourceItems];
          let insertIndex = 0;
          if (targetItemId) {
            const overIndex = targetItems.findIndex(
              (item) => item.id === targetItemId
            );
            if (overIndex !== -1) {
              insertIndex = dropPosition === "top" ? overIndex : overIndex + 1;
            }
          }
          targetItems.splice(insertIndex, 0, movedItem);
          newColumns[targetColumnId] = targetItems;
        }

        return newColumns;
      });
    },
    [handleUpdateColumns]
  );

  const filteredMktUsers = useMemo(() => {
    if (!mktFilter) {
      return mktUsers;
    }
    const lowercasedFilter = mktFilter.toLowerCase();
    return mktUsers.filter((user) =>
      user.name.toLowerCase().includes(lowercasedFilter)
    );
  }, [mktUsers, mktFilter]);

  const renderColumn = useCallback(
    (columnId, title) => {
      const items = columns[columnId] || [];

      return (
        <div
          id={columnId}
          className={`card ${columnId} ${
            dragOverTarget.columnId === columnId && !dragOverTarget.itemId
              ? "is-drag-over"
              : ""
          }`}
          onDragOver={(e) => handleColumnDragOver(e, columnId)}
          onDrop={(e) => {
            e.preventDefault();
            handleDrop(e, columnId);
          }}
          onDragLeave={handleColumnDragLeave}
        >
          <p className="pcard">
            {title}
            <div className="items-length">
              <span>{items.length}</span>
            </div>
          </p>
          {isLoadingMktUsers && columnId === "card-1" && (
            <div className="loading-message">Loading MKT users...</div>
          )}
          {mktUsersError && columnId === "card-1" && (
            <div className="error-message">{mktUsersError}</div>
          )}

          {Array.isArray(items) && items.length === 0 ? (
            <div
              className="no-items"
              onDragOver={(e) => handleColumnDragOver(e, columnId)}
            >
              {columns[columnId] && columns[columnId].length === 0 && (
                <p className="no-results-message"></p>
              )}
            </div>
          ) : (
            Array.isArray(items) &&
            items.map((item) => (
              <div
                key={item.id}
                id={item.id}
                className={`movable-item
                ${draggingItemData?.itemId === item.id ? "is-dragging" : ""}
                ${
                  dragOverTarget.itemId === item.id &&
                  dragOverTarget.position === "top"
                    ? "is-drag-over-top"
                    : ""
                }
                ${
                  dragOverTarget.itemId === item.id &&
                  dragOverTarget.position === "bottom"
                    ? "is-drag-over-bottom"
                    : ""
                }
                `}
                draggable={columnId !== "card-2" && columnId !== "card-4"}
                onDragStart={(e) => handleDragStart(e, item.id, columnId)}
                onDragEnd={handleDragEnd}
                onDragOver={(e) => handleItemDragOver(e, item.id, columnId)}
                onDrop={(e) => {
                  e.stopPropagation();
                  handleDrop(e, columnId, item.id, dragOverTarget.position);
                }}
                onClick={() => handleCardClick(item.content)}
              >
                {typeof item.content === "object" &&
                item.content !== null &&
                (item.content.type === "Referral" ||
                  item.content.type === "Soulver") ? (
                  <div className="custom-card-content">
                    <div className="card-heade">
                      <span className="card-name">{item.content.name}</span>
                      <br />
                      <span className="emails">{item.content.email}</span>
                      <br />
                      <span className="telefonos">
                        {item.content.phoneNumber}
                      </span>
                    </div>
                    <div
                      style={{
                        display: "flex",
                        flexDirection: "column",
                        position: "relative",
                      }}
                      className="card-referred-by"
                    >
                      <span className="label">
                        {item.content.type === "Referral"
                          ? "Referred by:"
                          : "Type:"}{" "}
                      </span>
                      <span className="value">
                        {item.content.type === "Referral"
                          ? item.content.referredBy
                          : item.content.type}
                      </span>
                    </div>
                    {columnId === "card-4" && (
                      <div className="checkbox">
                        <labe>
                          Recoverable
                          <input
                            type="checkbox"
                            onClick={(e) => e.stopPropagation()}
                          />
                        </labe>
                      </div>
                    )}
                    <div className="card-foote">
                      <span className="card-dat">{item.content.date}</span>
                      {item.content.creationDate && (
                        <div className="card-met">
                          {formatTimeElapsed(item.content.creationDate)}
                        </div>
                      )}

                      <div
                        className="card-initials-display"
                        onClick={(e) => handleMktInitialsClick(e, item.id)} // Evita que este clic abra el modal de la tarjeta
                        style={{
                          backgroundColor: item.content.selectedMktUser
                            ? COLORS_ARRAY[
                                mktUsers.findIndex(
                                  (user) =>
                                    user.id === item.content.selectedMktUser.id
                                ) % COLORS_ARRAY.length
                              ]
                            : item.content.assignedMktUserId
                            ? COLORS_ARRAY[
                                mktUsers.findIndex(
                                  (user) =>
                                    user.id === item.content.assignedMktUserId
                                ) % COLORS_ARRAY.length
                              ]
                            : "#999",
                          cursor: "pointer",
                        }}
                        data-tooltip-id={`mkt-tooltip-${item.id}`}
                        data-tooltip-content={
                          item.content.selectedMktUser
                            ? item.content.selectedMktUser.name
                            : item.content.assignedMktUserId
                            ? mktUsers.find(
                                (user) =>
                                  user.id === item.content.assignedMktUserId
                              )?.name || "Unassigned Mkt User"
                            : "Select MKT User"
                        }
                        data-tooltip-place="bottom"
                      >
                        <h6>
                          {item.content.selectedMktUser
                            ? getInitials(item.content.selectedMktUser.name)
                            : item.content.assignedMktUserId
                            ? getInitials(
                                mktUsers.find(
                                  (user) =>
                                    user.id === item.content.assignedMktUserId
                                )?.name
                              )
                            : "??"}
                        </h6>
                      </div>
                    </div>

                    <div
                      className="card-initials-selector-container"
                      style={{
                        position: "absolute",
                        bottom: "10px",
                        right: "10px",
                      }}
                    >
                      <Tooltip id={`mkt-tooltip-${item.id}`} />

                      {openMktSelectorItemId === item.id && (
                        <div
                          className="mkt-users-dropdown"
                          ref={(el) => (selectorRefs.current[item.id] = el)}
                        >
                          <input
                            style={{ marginLeft: "40px" }}
                            type="text"
                            placeholder="Filter users..."
                            value={mktFilter}
                            onChange={(e) => setMktFilter(e.target.value)}
                            onClick={(e) => e.stopPropagation()}
                          />
                          {isLoadingMktUsers && (
                            <div
                              style={{ padding: "10px", textAlign: "center" }}
                            >
                              Loading users...
                            </div>
                          )}
                          {mktUsersError && !isLoadingMktUsers && (
                            <div
                              style={{
                                padding: "10px",
                                textAlign: "center",
                                color: "red",
                              }}
                            >
                              {mktUsersError}
                            </div>
                          )}
                          {!isLoadingMktUsers &&
                            !mktUsersError &&
                            filteredMktUsers.length > 0 && (
                              <ul>
                                {filteredMktUsers.map((user, idx) => (
                                  <li
                                    key={user.id}
                                    onClick={(e) => {
                                      e.stopPropagation();
                                      handleMktUserSelect(
                                        columnId,
                                        item.id,
                                        user
                                      );
                                    }}
                                    className={
                                      item.content.selectedMktUser &&
                                      item.content.selectedMktUser.id ===
                                        user.id
                                        ? "selected"
                                        : ""
                                    }
                                  >
                                    <div
                                      className="mkt-user-option-initials"
                                      style={{
                                        backgroundColor:
                                          COLORS_ARRAY[
                                            idx % COLORS_ARRAY.length
                                          ],
                                      }}
                                    >
                                      {getInitials(user.name)}
                                    </div>
                                    <span className="mkt-user-option-name">
                                      {user.name}
                                    </span>
                                  </li>
                                ))}
                              </ul>
                            )}
                          {!isLoadingMktUsers &&
                            !mktUsersError &&
                            filteredMktUsers.length === 0 && (
                              <div
                                style={{
                                  padding: "10px",
                                  textAlign: "center",
                                }}
                              >
                                No MKT users found.
                              </div>
                            )}
                        </div>
                      )}
                    </div>
                  </div>
                ) : (
                  item.content
                )}
              </div>
            ))
          )}
        </div>
      );
    },
    [
      columns,
      dragOverTarget,
      draggingItemData,
      handleDrop,
      handleDragStart,
      handleDragEnd,
      handleColumnDragOver,
      handleColumnDragLeave,
      handleItemDragOver,
      isLoadingMktUsers,
      mktUsersError,
      mktUsers,
      openMktSelectorItemId,
      handleMktInitialsClick,
      handleMktUserSelect,
      mktFilter,
      filteredMktUsers,
      handleCardClick,
    ]
  );

  return (
    <>
      <div className="flex-cotent">
        {renderColumn("card-1", "New")}
        {renderColumn("card-4", "In Progress")}
        {renderColumn("card-5", "Other Process")}
        {renderColumn("card-3", "Rejected")}
        {renderColumn("card-2", "Banned")}
      </div>
      {selectedCard && (
        // eslint-disable-next-line react/jsx-pascal-case
        <ModalDetalleCardsMKT_RECR
          isActive={isActive}
          card={selectedCard}
          onClose={handleCloseModal}
          mktUsers={mktUsers}
          view="mkt"
        />
      )}
    </>
  );
}

export default CardMkt;
