package model.service;

import abstracter.Direction;
import abstracter.WallDirection;
import model.service.GameModelService;

public interface ChessBoardModelService {
	/**
	 * ��ʼ������
	 * @return
	 */
	public boolean initialize(int height,int width,int wallNum,int playerNum);
	
	/**
	 * ����GameModelService���ã���ʼ��ʱʹ��
	 * @param gameModel
	 */
	public void setGameModel(GameModelService gameModel);
	
	/**
	 * �ƶ�����
	 * @param direction
	 * @param playerNo
	 * @return
	 */
	public boolean move(int playerNo,Direction direction);
	
	/**
	 * ����һ��ǽ
	 * @param x
	 * @param y
	 * @return
	 */
	public boolean set(int playerNo,int x,int y,WallDirection wallDirection);
}
