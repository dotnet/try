import { Action, SET_WORKSPACE_INFO } from "../constants/ActionTypes";
import { IWorkspaceInfo } from "../IState";

const initialState: IWorkspaceInfo = {
    originType: "undefinedOrigin"
};

export default function workspaceInfoReducer(state: IWorkspaceInfo = initialState, action: Action): IWorkspaceInfo {
    if (!action) {
        return state;
    }
    switch (action.type) {
        case SET_WORKSPACE_INFO: {
            return {
                ...action.workspaceInfo
            };
        }
        default:
            return state;
    }
}
