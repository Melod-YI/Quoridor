package controller.service;

import abstracter.Direction;
import abstracter.WallDirection;

public interface GameControllerService {
	public boolean handMove(int playerNo,Direction direction);
	public boolean handSet(int playerNo,int x,int y,WallDirection wallDirection);
}
