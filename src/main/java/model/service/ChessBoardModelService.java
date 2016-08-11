package model.service;

import abstracter.Direction;
import abstracter.WallDirection;
import model.service.GameModelService;

public interface ChessBoardModelService {
	/**
	 * 初始化棋盘
	 * @return
	 */
	public boolean initialize(int height,int width,int wallNum,int playerNum);
	
	/**
	 * 传入GameModelService引用，初始化时使用
	 * @param gameModel
	 */
	public void setGameModel(GameModelService gameModel);
	
	/**
	 * 移动棋子
	 * @param direction
	 * @param playerNo
	 * @return
	 */
	public boolean move(int playerNo,Direction direction);
	
	/**
	 * 设置一面墙
	 * @param x
	 * @param y
	 * @return
	 */
	public boolean set(int playerNo,int x,int y,WallDirection wallDirection);
	
	
	/**
	 * 控制台输出棋盘格状态（单元测试用）
	 */
	public void wallPrint();
}
