package model.po;
//待修改
public class StatisticPO {
	private int PlayerNum;
	private int wins;
	
	public StatisticPO() {
		super();
	}

	public StatisticPO(int PlayerNum,int wins){
		super();
		this.setPlayerNum(PlayerNum);
		this.setWins(wins);
	}

	public int getPlayerNum() {
		return PlayerNum;
	}

	public void setPlayerNum(int playerNum) {
		PlayerNum = playerNum;
	}

	public int getWins() {
		return wins;
	}

	public void setWins(int wins) {
		this.wins = wins;
	}

}
